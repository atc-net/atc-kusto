namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// The Ingest V2 backed implementation of <see cref="IKustoIngestClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the only file in the library that references Kusto Ingest V2 source, property and
/// operation types, so SDK churn stays contained here.
/// </para>
/// <para>
/// Building the V2 ingest clients performs no network I/O. The exception is the user-containers
/// uploader (queued/managed with <see cref="AtcKustoOptions.IngestUploadContainers"/>), whose
/// <c>Build()</c> synchronously acquires a token from the credential. Instances are cached by the
/// provider, so that cost is paid once per connection and mode, and a credential failure surfaces
/// from construction.
/// </para>
/// </remarks>
internal sealed class KustoIngestClient : IKustoIngestClient
{
    private readonly IIngest client;
    private readonly IUploader? uploader;

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoIngestClient"/> class, building the V2
    /// client for <paramref name="mode"/>.
    /// </summary>
    /// <param name="clusterUri">The cluster URI, e.g. <c>https://mycluster.westeurope.kusto.windows.net</c>.</param>
    /// <param name="credential">The credential used for the cluster and for any upload containers.</param>
    /// <param name="uploadContainers">
    /// Blob containers that queued and managed-streaming ingestion upload to, typically privately
    /// reachable containers for clusters behind a private endpoint. Empty uses the SDK's default
    /// Kusto-internal storage. Ignored for <see cref="IngestionMode.Streaming"/>.
    /// </param>
    /// <param name="mode">The ingestion mode the client is built for.</param>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode"/> is not a defined value.</exception>
    public KustoIngestClient(
        Uri clusterUri,
        TokenCredential credential,
        IList<Uri> uploadContainers,
        IngestionMode mode)
    {
        ArgumentNullException.ThrowIfNull(clusterUri);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(uploadContainers);

        uploader = CreateUploader(uploadContainers, credential, mode);

        try
        {
            client = CreateClient(clusterUri, credential, mode, uploader);
        }
        catch
        {
            uploader?.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The payload is sent uncompressed. The stream is left open and its position is not reset, so
    /// the caller should pass it positioned at the start of the data.
    /// </remarks>
    /// <exception cref="IngestException">
    /// Thrown by the SDK when the service rejects the request or it cannot be delivered; derived
    /// types identify the cause (for example <see cref="IngestSizeLimitExceededException"/>).
    /// </exception>
    public async Task<KustoIngestionResult> IngestStreamAsync(
        Stream data,
        KustoIngestFormat format,
        string database,
        string table,
        string? mappingReference,
        bool enableTracking,
        IngestionMode mode,
        CancellationToken cancellationToken)
    {
        // leaveOpen: the caller owns the stream; disposing the source must not close it.
        using var source = new StreamSource(
            data,
            DataSourceCompressionType.None,
            format.ToDataSourceFormat(),
            leaveOpen: true);

        return await IngestAsync(
            source,
            database,
            table,
            mappingReference,
            enableTracking,
            mode,
            cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The service reads the blob itself, so the URI must be readable by the cluster (SAS token or
    /// RBAC). Compression is inferred by the service, for example from a <c>.gz</c> extension.
    /// </remarks>
    /// <exception cref="IngestException">
    /// Thrown by the SDK when the service rejects the request or it cannot be delivered.
    /// </exception>
    public async Task<KustoIngestionResult> IngestBlobAsync(
        Uri blobUri,
        KustoIngestFormat format,
        string database,
        string table,
        string? mappingReference,
        bool enableTracking,
        IngestionMode mode,
        CancellationToken cancellationToken)
    {
        // OriginalString keeps a SAS query string exactly as supplied. Compression is left
        // unspecified so the service can infer it from the blob, e.g. a .gz extension.
        using var source = new BlobSource(
            blobUri.OriginalString,
            format.ToDataSourceFormat());

        return await IngestAsync(
            source,
            database,
            table,
            mappingReference,
            enableTracking,
            mode,
            cancellationToken);
    }

    /// <summary>
    /// Disposes the V2 client and, when present, the upload-containers uploader.
    /// </summary>
    public void Dispose()
    {
        client.Dispose();

        // The client never disposes the uploader: ManagedStreaming has no such option and Queued is
        // built with shouldDisposeUploader: false, so ownership stays in one place.
        uploader?.Dispose();
    }

    /// <summary>
    /// Maps how the service actually handled the request onto the Atc status.
    /// </summary>
    /// <remarks>
    /// The status must come from the operation, not the requested mode: ManagedStreaming can fall
    /// back to queued ingestion, which is not yet complete.
    /// </remarks>
    /// <param name="method">The ingestion method reported by the service.</param>
    /// <returns>
    /// <see cref="KustoIngestionStatus.Succeeded"/> for streaming (the rows are in the table when the
    /// call returns); otherwise <see cref="KustoIngestionStatus.Queued"/> (accepted, processed later).
    /// </returns>
    internal static KustoIngestionStatus ToIngestionStatus(
        IngestionMethod method)
        => method switch
        {
            IngestionMethod.Streaming => KustoIngestionStatus.Succeeded,

            // Queued, or any method a newer SDK might add: accepted, but not confirmed complete.
            _ => KustoIngestionStatus.Queued,
        };

    private async Task<KustoIngestionResult> IngestAsync(
        IngestionSource source,
        string database,
        string table,
        string? mappingReference,
        bool enableTracking,
        IngestionMode mode,
        CancellationToken cancellationToken)
    {
        var properties = new IngestProperties
        {
            IngestionMappingReference = mappingReference,
            EnableTracking = enableTracking,
        };

        var operation = await client.IngestAsync(
            source,
            database,
            table,
            properties,
            cancellationToken);

        return new KustoIngestionResult
        {
            Status = ToIngestionStatus(operation.IngestionMethod),
            Mode = mode,
            OperationId = operation.Id,
            OperationHandle = enableTracking
                ? operation.ToJsonString()
                : null,
        };
    }

    private static IUploader? CreateUploader(
        IList<Uri> uploadContainers,
        TokenCredential credential,
        IngestionMode mode)
    {
        // Streaming uploads nothing; an empty list means the SDK's default (Kusto-internal) storage.
        if (mode == IngestionMode.Streaming ||
            uploadContainers.Count == 0)
        {
            return null;
        }

        var builder = UserContainersUploaderBuilder.Create();
        foreach (var container in uploadContainers)
        {
            builder = builder.AddContainer(container.OriginalString, credential);
        }

        return builder.Build();
    }

    private static IIngest CreateClient(
        Uri clusterUri,
        TokenCredential credential,
        IngestionMode mode,
        IUploader? uploader)
    {
        switch (mode)
        {
            case IngestionMode.Streaming:
                return StreamingIngestClientBuilder
                    .Create(clusterUri)
                    .WithAuthentication(credential)
                    .Build();

            case IngestionMode.ManagedStreaming:
            {
                // Streaming ingestion must be enabled on the cluster and via a streaming policy on the
                // database/table. When it isn't, the V2 default (false) makes ManagedStreaming throw.
                // We set true so it falls back to queued instead, matching IngestionMode.ManagedStreaming's
                // documented "streaming with fallback to queued"; the result then reports Queued.
                // Callers who want a hard failure use IngestionMode.Streaming.
                var builder = ManagedStreamingIngestClientBuilder
                    .Create(clusterUri)
                    .WithAuthentication(credential)
                    .WithManagedStreamingPolicy(new ManagedStreamingPolicy
                    {
                        ContinueWhenStreamingIngestionUnavailable = true,
                    });

                return uploader is null
                    ? builder.Build()
                    : builder.WithUploader(uploader).Build();
            }

            case IngestionMode.Queued:
            {
                var builder = QueuedIngestClientBuilder
                    .Create(clusterUri)
                    .WithAuthentication(credential);

                return uploader is null
                    ? builder.Build()
                    : builder.WithUploader(uploader, shouldDisposeUploader: false).Build();
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported ingestion mode.");
        }
    }
}