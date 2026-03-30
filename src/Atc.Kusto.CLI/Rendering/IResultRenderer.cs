namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Renders tabular result data in a specific output format.
/// </summary>
public interface IResultRenderer
{
    /// <summary>
    /// Renders the given columns and rows to the console.
    /// </summary>
    /// <param name="columns">The column names.</param>
    /// <param name="rows">The data rows.</param>
    void Render(
        IReadOnlyList<string> columns,
        IReadOnlyList<string[]> rows);

    /// <summary>
    /// Renders query statistics as key-value pairs.
    /// </summary>
    /// <param name="statistics">The flattened statistics dictionary.</param>
    void RenderStatistics(IDictionary<string, string> statistics);

    /// <summary>
    /// Renders a web explorer URL for opening the query in Azure Data Explorer.
    /// </summary>
    /// <param name="url">The web explorer deep-link URL.</param>
    void RenderWebExplorerUrl(Uri url);
}