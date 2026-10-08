namespace Atc.Kusto;

/// <summary>
/// Represents the outcome of an ingestion request.
/// </summary>
public enum KustoIngestionStatus
{
    /// <summary>
    /// The data was ingested and is queryable.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The data was accepted and queued for batched ingestion, but is not yet queryable.
    /// </summary>
    /// <remarks>
    /// This is the expected outcome for <see cref="IngestionMode.Queued"/>, and for
    /// <see cref="IngestionMode.ManagedStreaming"/> when it falls back to queued ingestion. The data
    /// is normally queryable within minutes, depending on the table's batching policy; it can still
    /// fail later, which only tracking (<see cref="KustoIngestTarget.EnableTracking"/>) reveals.
    /// </remarks>
    Queued,

    /// <summary>
    /// The ingestion request failed.
    /// </summary>
    /// <remarks>
    /// See <see cref="KustoIngestionResult.ErrorMessage"/> for details.
    /// </remarks>
    Failed,
}