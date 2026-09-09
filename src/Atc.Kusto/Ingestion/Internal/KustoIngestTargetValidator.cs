namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// Validates an ingestion target before any I/O is performed.
/// </summary>
/// <remarks>
/// Validation runs against the resolved mode, so the caller must substitute the connection
/// default before calling. Invalid input is a caller mistake and therefore throws, unlike
/// operational failures which are reported on <see cref="KustoIngestionResult"/>.
/// </remarks>
internal static class KustoIngestTargetValidator
{
    /// <summary>
    /// The absolute streaming ingestion request cap, which applies regardless of format or
    /// compression.
    /// </summary>
    /// <remarks>
    /// Re-verify this against the current Azure Data Explorer service limits if they change.
    /// </remarks>
    public const long StreamingIngestionMaxBytes = 10L * 1024 * 1024;

    /// <summary>
    /// Validates the target for the given resolved mode and payload.
    /// </summary>
    /// <param name="target">The ingestion target.</param>
    /// <param name="resolvedMode">The mode after applying the connection default.</param>
    /// <param name="knownPayloadByteLength">
    /// The payload length when it can be determined, otherwise <see langword="null"/>. A
    /// non-seekable stream cannot be measured, so the size guard is skipped for it.
    /// </param>
    /// <param name="isInlineRows">Whether the call came from the in-memory rows overload.</param>
    /// <exception cref="ArgumentNullException">Thrown when the target is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the target is not valid for the resolved mode.</exception>
    public static void Validate(
        KustoIngestTarget target,
        IngestionMode resolvedMode,
        long? knownPayloadByteLength,
        bool isInlineRows)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (string.IsNullOrWhiteSpace(target.TableName))
        {
            throw new ArgumentException(
                "TableName must be provided.",
                nameof(target));
        }

        if (target.Format.IsJson() &&
            string.IsNullOrWhiteSpace(target.MappingReference))
        {
            throw new ArgumentException(
                "A MappingReference is required for the Json and MultiJson formats.",
                nameof(target));
        }

        if (isInlineRows && !target.Format.IsJson())
        {
            throw new ArgumentException(
                "The inline rows overload serializes to multijson, so Format must be Json or MultiJson.",
                nameof(target));
        }

        if (target.EnableTracking &&
            resolvedMode == IngestionMode.Streaming)
        {
            throw new ArgumentException(
                "EnableTracking is only meaningful for the ManagedStreaming and Queued modes, not pure Streaming.",
                nameof(target));
        }

        if (resolvedMode == IngestionMode.Streaming &&
            knownPayloadByteLength is { } length &&
            length > StreamingIngestionMaxBytes)
        {
            throw new ArgumentException(
                $"The streaming ingestion payload of {length} bytes exceeds the 10 MB limit. Use ManagedStreaming or Queued instead.",
                nameof(target));
        }
    }
}