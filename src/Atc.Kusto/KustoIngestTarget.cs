namespace Atc.Kusto;

/// <summary>
/// Describes a single ingestion request: the destination table, the source data format,
/// and optional mode/mapping/connection overrides.
/// </summary>
public sealed record KustoIngestTarget
{
    /// <summary>
    /// Gets the destination table name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets the format of the source data.
    /// </summary>
    /// <remarks>
    /// Inline rows are always written as <see cref="KustoIngestFormat.MultiJson"/>.
    /// </remarks>
    public required KustoIngestFormat Format { get; init; }

    /// <summary>
    /// Gets the server-side ingestion mapping name.
    /// </summary>
    /// <remarks>
    /// Required for <see cref="KustoIngestFormat.Json"/> and
    /// <see cref="KustoIngestFormat.MultiJson"/>; optional for the CSV-family formats.
    /// </remarks>
    public string? MappingReference { get; init; }

    /// <summary>
    /// Gets the ingestion mode.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the connection's configured default ingestion mode is used.
    /// </remarks>
    public IngestionMode? Mode { get; init; }

    /// <summary>
    /// Gets the named-options connection to use.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the default connection is selected.
    /// </remarks>
    public string? ConnectionName { get; init; }

    /// <summary>
    /// Gets the database override.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the connection's configured database is used.
    /// </remarks>
    public string? DatabaseName { get; init; }

    /// <summary>
    /// Gets a value indicating whether operation tracking is enabled.
    /// </summary>
    /// <remarks>
    /// Applies to the queued and managed-streaming modes only, and is off by default.
    /// </remarks>
    public bool EnableTracking { get; init; }
}