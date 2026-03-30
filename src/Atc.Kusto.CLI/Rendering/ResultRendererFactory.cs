namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Creates the appropriate <see cref="IResultRenderer"/> for a given output format.
/// </summary>
public static class ResultRendererFactory
{
    /// <summary>
    /// Creates a renderer for the specified output format.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <returns>A renderer instance.</returns>
    public static IResultRenderer Create(OutputFormat format)
        => format switch
        {
            OutputFormat.Json => new JsonResultRenderer(),
            OutputFormat.Markdown => new MarkdownResultRenderer(),
            OutputFormat.Csv => new CsvResultRenderer(),
            _ => new HumanResultRenderer(),
        };
}