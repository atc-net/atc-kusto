namespace Atc.Kusto;

/// <summary>
/// Specifies the data format of an ingestion payload.
/// </summary>
/// <remarks>
/// This is an Atc-owned abstraction so that the Kusto SDK data-source format type never appears
/// in the public API surface.
/// </remarks>
public enum KustoIngestFormat
{
    /// <summary>
    /// A stream of JSON objects, one per line.
    /// </summary>
    /// <remarks>
    /// Requires a mapping reference. This is the format produced when ingesting in-memory rows.
    /// </remarks>
    MultiJson,

    /// <summary>
    /// A single JSON payload.
    /// </summary>
    /// <remarks>
    /// Requires a mapping reference.
    /// </remarks>
    Json,

    /// <summary>
    /// Comma-separated values.
    /// </summary>
    /// <remarks>
    /// A mapping reference is optional.
    /// </remarks>
    Csv,

    /// <summary>
    /// Tab-separated values.
    /// </summary>
    /// <remarks>
    /// A mapping reference is optional.
    /// </remarks>
    Tsv,
}