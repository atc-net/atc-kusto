namespace Atc.Kusto;

/// <summary>
/// The state of a tracked (queued) ingestion operation, as reported by
/// <see cref="IKustoIngestor.GetIngestionStatusAsync"/>.
/// </summary>
public enum KustoIngestionOperationStatus
{
    /// <summary>
    /// The cluster has not finished processing the data yet. Check again later.
    /// </summary>
    InProgress,

    /// <summary>
    /// All data was ingested and is queryable.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Some sources were ingested and some failed; see <see cref="KustoIngestionOperationResult.Errors"/>.
    /// </summary>
    PartialSuccess,

    /// <summary>
    /// The ingestion failed; see <see cref="KustoIngestionOperationResult.Errors"/>.
    /// </summary>
    Failed,

    /// <summary>
    /// The ingestion was cancelled by the service.
    /// </summary>
    Cancelled,
}