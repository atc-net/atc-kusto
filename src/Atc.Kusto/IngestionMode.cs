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
    /// Requires a streaming ingestion policy on the target table, a mapping reference, and a
    /// payload of at most 10 MB.
    /// </remarks>
    Streaming,

    /// <summary>
    /// Attempts streaming ingestion and falls back to queued ingestion automatically.
    /// </summary>
    /// <remarks>
    /// Fallback occurs when the payload is too large or streaming is unavailable. This is the
    /// default mode.
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