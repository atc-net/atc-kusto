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

        ValidatePayloadSize(resolvedMode, knownPayloadByteLength, nameof(target));
    }

    /// <summary>
    /// Validates the payload size for the resolved mode.
    /// </summary>
    /// <remarks>
    /// Only <see cref="IngestionMode.Streaming"/> has a hard cap; ManagedStreaming reroutes oversize
    /// payloads to queued ingestion, and queued limits are far larger.
    /// </remarks>
    /// <param name="resolvedMode">The mode after applying the connection default.</param>
    /// <param name="knownPayloadByteLength">The payload length, or <see langword="null"/> when unknown (no check).</param>
    /// <param name="paramName">The caller's parameter that carries the payload, reported on the exception.</param>
    /// <exception cref="ArgumentException">Thrown when a streaming payload exceeds <see cref="StreamingIngestionMaxBytes"/>.</exception>
    public static void ValidatePayloadSize(
        IngestionMode resolvedMode,
        long? knownPayloadByteLength,
        string paramName)
    {
        if (resolvedMode == IngestionMode.Streaming &&
            knownPayloadByteLength is { } length and > StreamingIngestionMaxBytes)
        {
            throw new ArgumentException(
                $"The streaming ingestion payload of {length} bytes exceeds the 10 MB limit. Use ManagedStreaming or Queued instead.",
                paramName);
        }
    }

    /// <summary>
    /// Validates that a connection is configured in a shape that supports ingestion.
    /// </summary>
    /// <remarks>
    /// The Ingest V2 builders take a cluster URI and a <see cref="TokenCredential"/>, so a connection
    /// configured only with a connection string, or without a credential, can query but not ingest.
    /// This is a configuration error, so it throws rather than being reported as a failed result.
    /// </remarks>
    /// <param name="options">The resolved connection options.</param>
    /// <param name="connectionName">The connection name, used in the message; <see langword="null"/> for the default.</param>
    /// <exception cref="InvalidOperationException">Thrown when HostAddress or Credential is missing.</exception>
    public static void ValidateConnection(
        AtcKustoOptions options,
        string? connectionName)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.HostAddress is null ||
            options.Credential is null)
        {
            throw new InvalidOperationException(
                $"Ingestion requires both HostAddress and Credential for kusto connection: {connectionName ?? "(default)"}. " +
                "ConnectionString-only or credential-less configurations are not supported for ingestion.");
        }
    }
}