namespace Atc.Kusto;

/// <summary>
/// Describes why a source in a tracked ingestion operation failed or was cancelled.
/// </summary>
public sealed record KustoIngestionOperationError
{
    /// <summary>
    /// Gets the Kusto ingestion error code, for example <c>BadRequest_EmptyBlob</c> or <c>Timeout</c>.
    /// </summary>
    /// <remarks>
    /// <c>Unknown</c> when the service did not report a code. See the Azure Data Explorer
    /// "Ingestion error codes" documentation for the full list.
    /// </remarks>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Gets a value indicating whether the service classified the failure as transient, meaning
    /// re-ingesting the same data may succeed. Permanent failures (bad data, missing mapping, …) won't.
    /// </summary>
    public bool IsTransient { get; init; }

    /// <summary>
    /// Gets the service's description of the failure, when available.
    /// </summary>
    public string? Details { get; init; }
}