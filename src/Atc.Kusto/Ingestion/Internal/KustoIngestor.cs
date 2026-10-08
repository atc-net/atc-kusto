namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// Orchestrates an ingestion request: validates it, prepares the payload, and hands it to the
/// cached <see cref="IKustoIngestClient"/> for the resolved connection and mode.
/// </summary>
/// <remarks>
/// <para>
/// Error contract: invalid arguments throw <see cref="ArgumentException"/>, a connection that is not
/// configured for ingestion throws <see cref="InvalidOperationException"/>, and cancellation of the
/// caller's token throws <see cref="OperationCanceledException"/>. Everything else, such as service
/// rejections, network or credential failures, is reported as <see cref="KustoIngestionStatus.Failed"/>.
/// </para>
/// <para>
/// There is deliberately no retry layer here: the Ingest V2 SDK retries transient failures itself,
/// and retrying a write on top of that risks duplicate ingestion.
/// </para>
/// </remarks>
internal sealed partial class KustoIngestor : IKustoIngestor
{
    private readonly IKustoIngestClientProvider clientProvider;
    private readonly IOptionsMonitor<AtcKustoOptions> optionsMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoIngestor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clientProvider">Provides cached ingest clients per connection and mode.</param>
    /// <param name="optionsMonitor">Resolves <see cref="AtcKustoOptions"/> per named connection.</param>
    public KustoIngestor(
        ILogger<KustoIngestor> logger,
        IKustoIngestClientProvider clientProvider,
        IOptionsMonitor<AtcKustoOptions> optionsMonitor)
    {
        this.logger = logger;
        this.clientProvider = clientProvider;
        this.optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    public async Task<KustoIngestionResult> IngestAsync<T>(
        IEnumerable<T> rows,
        KustoIngestTarget target,
        JsonSerializerOptions? serializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var (mode, database) = Resolve(target, isInlineRows: true);

        await using var stream = MultiJsonSerializer.Serialize(rows, serializerOptions, cancellationToken);
        if (stream.Length == 0)
        {
            return Skipped(target, mode);
        }

        KustoIngestTargetValidator.ValidatePayloadSize(mode, stream.Length, nameof(rows));

        return await ExecuteAsync(
            (client, ct) => client.IngestStreamAsync(
                stream,
                target.Format,
                database,
                target.TableName,
                target.MappingReference,
                target.EnableTracking,
                mode,
                ct),
            target,
            mode,
            database,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<KustoIngestionResult> IngestAsync(
        Stream data,
        KustoIngestTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (!data.CanSeek)
        {
            throw new ArgumentException(
                "The stream must be seekable so its size can be validated and the SDK can read it. " +
                "Copy a forward-only stream into a MemoryStream or a temporary FileStream first.",
                nameof(data));
        }

        var (mode, database) = Resolve(target, isInlineRows: false);

        var remainingBytes = data.Length - data.Position;
        if (remainingBytes == 0)
        {
            return Skipped(target, mode);
        }

        KustoIngestTargetValidator.ValidatePayloadSize(mode, remainingBytes, nameof(data));

        return await ExecuteAsync(
            (client, ct) => client.IngestStreamAsync(
                data,
                target.Format,
                database,
                target.TableName,
                target.MappingReference,
                target.EnableTracking,
                mode,
                ct),
            target,
            mode,
            database,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<KustoIngestionResult> IngestFromBlobAsync(
        Uri blobUri,
        KustoIngestTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blobUri);

        if (!blobUri.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The blob URI must be absolute.",
                nameof(blobUri));
        }

        var (mode, database) = Resolve(target, isInlineRows: false);

        return ExecuteAsync(
            (client, ct) => client.IngestBlobAsync(
                blobUri,
                target.Format,
                database,
                target.TableName,
                target.MappingReference,
                target.EnableTracking,
                mode,
                ct),
            target,
            mode,
            database,
            cancellationToken);
    }

    /// <summary>
    /// Resolves the effective mode and database and validates the request, before any payload work.
    /// </summary>
    private (IngestionMode Mode, string Database) Resolve(
        KustoIngestTarget target,
        bool isInlineRows)
    {
        ArgumentNullException.ThrowIfNull(target);

        var options = optionsMonitor.Get(target.ConnectionName);
        var mode = target.Mode ?? options.DefaultIngestionMode;

        KustoIngestTargetValidator.Validate(target, mode, knownPayloadByteLength: null, isInlineRows);

        var database = target.DatabaseName ?? options.DatabaseName;
        if (string.IsNullOrWhiteSpace(database))
        {
            throw new ArgumentException(
                "No database could be resolved. Set KustoIngestTarget.DatabaseName or AtcKustoOptions.DatabaseName.",
                nameof(target));
        }

        KustoIngestTargetValidator.ValidateConnection(options, target.ConnectionName);

        return (mode, database);
    }

    private KustoIngestionResult Skipped(
        KustoIngestTarget target,
        IngestionMode mode)
    {
        LogIngestionSkipped(target.TableName, mode);

        return new KustoIngestionResult
        {
            Status = KustoIngestionStatus.Skipped,
            Mode = mode,
        };
    }

    private async Task<KustoIngestionResult> ExecuteAsync(
        Func<IKustoIngestClient, CancellationToken, Task<KustoIngestionResult>> ingest,
        KustoIngestTarget target,
        IngestionMode mode,
        string database,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var activity = KustoDiagnostics.Source.StartActivity(
            KustoDiagnostics.ActivityNames.Ingest,
            ActivityKind.Client);

        activity?.SetTag(KustoDiagnostics.TagNames.IngestDatabase, database);
        activity?.SetTag(KustoDiagnostics.TagNames.IngestTable, target.TableName);
        activity?.SetTag(KustoDiagnostics.TagNames.IngestMode, mode.ToString());

        LogIngestionStarted(database, target.TableName, mode);
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            // Inside the try: building a client can fetch a token (user upload containers), and a
            // credential failure there is an operational failure like any other.
            var client = clientProvider.GetIngestClient(mode, target.ConnectionName);
            var result = await ingest(client, cancellationToken);

            var durationMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            if (result.Status == KustoIngestionStatus.Queued)
            {
                LogIngestionQueued(database, target.TableName, mode, result.OperationId, durationMs);
            }
            else
            {
                LogIngestionSucceeded(database, target.TableName, mode, result.OperationId, durationMs);
            }

            activity?.SetTag(KustoDiagnostics.TagNames.IngestStatus, result.Status.ToString());
            activity?.SetTag(KustoDiagnostics.TagNames.IngestOperationId, result.OperationId);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return result;
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested)
        {
            // Decided by the caller's token, not the exception type: the SDK may surface a
            // cancelled request as a transport or Kusto exception rather than an OperationCanceledException.
            activity?.SetStatus(ActivityStatusCode.Error, "Canceled");

            if (ex is OperationCanceledException)
            {
                throw;
            }

            throw new OperationCanceledException(
                "The ingestion was canceled.",
                ex,
                cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = ex.GetLastInnerMessage();
            LogIngestionFailed(ex, database, target.TableName, mode, errorMessage);

            activity?.SetTag(KustoDiagnostics.TagNames.IngestStatus, nameof(KustoIngestionStatus.Failed));
            activity?.SetStatus(ActivityStatusCode.Error, errorMessage);

            return new KustoIngestionResult
            {
                Status = KustoIngestionStatus.Failed,
                Mode = mode,
                ErrorMessage = errorMessage,
            };
        }
    }
}