namespace Atc.Kusto;

/// <summary>
/// The outcome of a tracked ingestion operation, as reported by
/// <see cref="IKustoIngestor.GetIngestionStatusAsync"/>.
/// </summary>
/// <remarks>
/// The counts are per source; an <see cref="IKustoIngestor"/> call ingests a single source, so for an
/// operation created by this library exactly one of them is normally 1.
/// </remarks>
public sealed record KustoIngestionOperationResult
{
    /// <summary>
    /// Gets the service's id for the operation (the same value as <see cref="KustoIngestionResult.OperationId"/>).
    /// </summary>
    public required string OperationId { get; init; }

    /// <summary>
    /// Gets the overall state of the operation.
    /// </summary>
    public required KustoIngestionOperationStatus Status { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation reached a final state, i.e. checking again won't change
    /// the outcome. <see langword="false"/> only for <see cref="KustoIngestionOperationStatus.InProgress"/>.
    /// </summary>
    public bool IsCompleted
        => Status != KustoIngestionOperationStatus.InProgress;

    /// <summary>
    /// Gets the number of sources still being processed.
    /// </summary>
    public int InProgressCount { get; init; }

    /// <summary>
    /// Gets the number of sources ingested successfully.
    /// </summary>
    public int SucceededCount { get; init; }

    /// <summary>
    /// Gets the number of sources that failed.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Gets the number of sources that were cancelled.
    /// </summary>
    public int CancelledCount { get; init; }

    /// <summary>
    /// Gets when the operation started (UTC).
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Gets when the service last updated the operation's state (UTC).
    /// </summary>
    public DateTime LastUpdateTime { get; init; }

    /// <summary>
    /// Gets the failures reported for the operation's sources; empty when nothing failed.
    /// </summary>
    public IReadOnlyList<KustoIngestionOperationError> Errors { get; init; } = [];
}