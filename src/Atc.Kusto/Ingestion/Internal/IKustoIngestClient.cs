namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// Internal seam over the Kusto Ingest V2 SDK.
/// </summary>
/// <remarks>
/// The single implementation confines every V2 type; everything on this interface is Atc- or
/// BCL-typed, so the rest of the library and its tests never touch the SDK. The interface is
/// <see cref="IDisposable"/> because the underlying V2 clients are.
/// </remarks>
internal interface IKustoIngestClient : IDisposable
{
    /// <summary>
    /// Ingests a stream payload.
    /// </summary>
    /// <param name="data">The payload. The stream is not disposed by the client.</param>
    /// <param name="format">The payload format.</param>
    /// <param name="database">The target database.</param>
    /// <param name="table">The target table.</param>
    /// <param name="mappingReference">The server-side mapping name, when required.</param>
    /// <param name="enableTracking">Whether to request an operation handle.</param>
    /// <param name="mode">The mode this client was built for, echoed onto the result.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<KustoIngestionResult> IngestStreamAsync(
        Stream data,
        KustoIngestFormat format,
        string database,
        string table,
        string? mappingReference,
        bool enableTracking,
        IngestionMode mode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the state of a tracked operation.
    /// </summary>
    /// <param name="operationHandle">A handle produced by this client's ingest methods with tracking enabled.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<KustoIngestionOperationResult> GetOperationStatusAsync(
        string operationHandle,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ingests from a blob URI.
    /// </summary>
    /// <param name="blobUri">The blob URI, which must be cluster-readable.</param>
    /// <param name="format">The payload format.</param>
    /// <param name="database">The target database.</param>
    /// <param name="table">The target table.</param>
    /// <param name="mappingReference">The server-side mapping name, when required.</param>
    /// <param name="enableTracking">Whether to request an operation handle.</param>
    /// <param name="mode">The mode this client was built for, echoed onto the result.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<KustoIngestionResult> IngestBlobAsync(
        Uri blobUri,
        KustoIngestFormat format,
        string database,
        string table,
        string? mappingReference,
        bool enableTracking,
        IngestionMode mode,
        CancellationToken cancellationToken);
}