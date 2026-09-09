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
    /// This is the expected outcome for <see cref="IngestionMode.Queued"/>.
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