namespace Atc.Kusto;

/// <summary>
/// Specifies how data is delivered to Azure Data Explorer during ingestion.
/// </summary>
public enum IngestionMode
{
    /// <summary>
    /// Sends the payload directly to the cluster for immediate ingestion.
    /// </summary>
    /// <remarks>
    /// When the call returns successfully the rows are in the table. Requires streaming ingestion
    /// to be enabled on the cluster and a streaming ingestion policy on the database or table, and a
    /// payload of at most 10 MB. Mappings must be referenced by name; inline mappings are not
    /// supported. Fails rather than falling back when streaming is unavailable.
    /// </remarks>
    Streaming,

    /// <summary>
    /// Attempts streaming ingestion and falls back to queued ingestion automatically.
    /// </summary>
    /// <remarks>
    /// Falls back when the payload is too large for streaming, after repeated transient streaming
    /// errors, or when streaming is not enabled for the cluster or table. The result reports which
    /// path was taken: <see cref="KustoIngestionStatus.Succeeded"/> when streamed,
    /// <see cref="KustoIngestionStatus.Queued"/> when it fell back. This is the default mode.
    /// </remarks>
    ManagedStreaming,

    /// <summary>
    /// Uploads the payload and queues it for batched ingestion.
    /// </summary>
    /// <remarks>
    /// Returns as soon as the data is queued, before it is queryable. Set
    /// <see cref="KustoIngestTarget.EnableTracking"/> to obtain an operation handle for later
    /// status checks.
    /// </remarks>
    Queued,
}