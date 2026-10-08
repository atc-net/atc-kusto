namespace Atc.Kusto;

/// <summary>
/// The mode-agnostic result of an ingestion request.
/// </summary>
/// <remarks>
/// Operational failures are reported here rather than thrown; only argument validation, a connection
/// not configured for ingestion, and cancellation raise exceptions. <b>Always check the result</b>:
/// inspect <see cref="IsSuccess"/> / <see cref="Status"/>, or call <see cref="EnsureSuccess"/> to throw
/// a <see cref="KustoIngestionException"/> on failure. Ignoring the result means a failed ingestion
/// goes unnoticed by the calling code (it is still logged at error level).
/// </remarks>
public sealed record KustoIngestionResult
{
    /// <summary>
    /// Gets the outcome of the ingestion request.
    /// </summary>
    public required KustoIngestionStatus Status { get; init; }

    /// <summary>
    /// Gets the ingestion mode actually used.
    /// </summary>
    /// <remarks>
    /// This is the mode after resolving the connection default, so it is never ambiguous.
    /// </remarks>
    public required IngestionMode Mode { get; init; }

    /// <summary>
    /// Gets the identifier the service assigned to the ingestion operation.
    /// </summary>
    /// <remarks>
    /// Useful for correlating logs with the cluster. Set whenever the request reached the service;
    /// <see langword="null"/> when the ingestion failed or was skipped before that.
    /// </remarks>
    public string? OperationId { get; init; }

    /// <summary>
    /// Gets a serialized operation handle for later tracking.
    /// </summary>
    /// <remarks>
    /// Present when <see cref="KustoIngestTarget.EnableTracking"/> is enabled; otherwise
    /// <see langword="null"/>.
    /// </remarks>
    public string? OperationHandle { get; init; }

    /// <summary>
    /// Gets the failure message.
    /// </summary>
    /// <remarks>
    /// Set when <see cref="Status"/> is <see cref="KustoIngestionStatus.Failed"/>; otherwise
    /// <see langword="null"/>.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the ingestion did not fail.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> for <see cref="KustoIngestionStatus.Succeeded"/>,
    /// <see cref="KustoIngestionStatus.Queued"/> (accepted, processed later) and
    /// <see cref="KustoIngestionStatus.Skipped"/> (nothing to send); <see langword="false"/> only for
    /// <see cref="KustoIngestionStatus.Failed"/>.
    /// </remarks>
    public bool IsSuccess => Status != KustoIngestionStatus.Failed;

    /// <summary>
    /// Throws a <see cref="KustoIngestionException"/> when the ingestion failed; otherwise returns this result.
    /// </summary>
    /// <remarks>
    /// Use it when a failure should stop the calling code, e.g.
    /// <c>(await ingestor.IngestAsync(rows, target)).EnsureSuccess();</c>.
    /// </remarks>
    /// <returns>This result, to allow chaining.</returns>
    /// <exception cref="KustoIngestionException">Thrown when <see cref="Status"/> is <see cref="KustoIngestionStatus.Failed"/>.</exception>
    public KustoIngestionResult EnsureSuccess()
        => IsSuccess
            ? this
            : throw new KustoIngestionException(this);
}