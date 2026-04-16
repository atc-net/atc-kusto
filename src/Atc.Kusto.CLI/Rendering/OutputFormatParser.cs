namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Parses output format strings to <see cref="OutputFormat"/> values.
/// </summary>
public static class OutputFormatParser
{
    /// <summary>
    /// Parses a format string into an <see cref="OutputFormat"/>.
    /// </summary>
    /// <param name="format">The format string (human, json, markdown, md).</param>
    /// <returns>The parsed output format.</returns>
    public static OutputFormat Parse(string format)
    {
        ArgumentNullException.ThrowIfNull(format);

        return format.ToLowerInvariant() switch
        {
            "json" => OutputFormat.Json,
            "markdown" or "md" => OutputFormat.Markdown,
            "csv" => OutputFormat.Csv,
            "tsv" => OutputFormat.Tsv,
            _ => OutputFormat.Human,
        };
    }
}