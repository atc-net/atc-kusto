namespace Atc.Kusto;

/// <summary>
/// The mode-agnostic result of an ingestion request.
/// </summary>
/// <remarks>
/// Operational failures are reported here rather than thrown; only argument validation and
/// cancellation raise exceptions.
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
}