namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// Maps the Atc-owned <see cref="KustoIngestFormat"/> onto the Kusto SDK format type.
/// </summary>
/// <remarks>
/// Keeping this mapping internal is what allows <see cref="KustoIngestFormat"/> to be the only
/// format type on the public API surface.
/// </remarks>
internal static class KustoIngestFormatExtensions
{
    /// <summary>
    /// Converts the format to the Kusto SDK equivalent.
    /// </summary>
    /// <param name="format">The format to convert.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not a supported format.</exception>
    public static DataSourceFormat ToDataSourceFormat(
        this KustoIngestFormat format)
        => format switch
        {
            KustoIngestFormat.MultiJson => DataSourceFormat.multijson,
            KustoIngestFormat.Json => DataSourceFormat.json,
            KustoIngestFormat.Csv => DataSourceFormat.csv,
            KustoIngestFormat.Tsv => DataSourceFormat.tsv,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported ingestion format."),
        };

    /// <summary>
    /// Gets a value indicating whether the format belongs to the JSON family.
    /// </summary>
    /// <remarks>
    /// The JSON family requires a mapping reference, which is what callers use this for.
    /// </remarks>
    /// <param name="format">The format to test.</param>
    public static bool IsJson(this KustoIngestFormat format)
        => format is KustoIngestFormat.Json or KustoIngestFormat.MultiJson;
}