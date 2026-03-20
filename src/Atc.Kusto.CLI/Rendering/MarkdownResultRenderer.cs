namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Renders results as a GitHub Flavored Markdown table.
/// </summary>
public sealed class MarkdownResultRenderer : IResultRenderer
{
    /// <inheritdoc />
    public void Render(IReadOnlyList<string> columns, IReadOnlyList<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        var sb = new StringBuilder();

        // Header row
        sb.Append('|');
        foreach (var column in columns)
        {
            sb.Append(' ').Append(EscapeCell(column)).Append(" |");
        }

        sb.AppendLine();

        // Separator row
        sb.Append('|');
        for (var i = 0; i < columns.Count; i++)
        {
            sb.Append(" --- |");
        }

        sb.AppendLine();

        // Data rows
        foreach (var row in rows)
        {
            sb.Append('|');
            foreach (var cell in row)
            {
                sb.Append(' ').Append(EscapeCell(cell)).Append(" |");
            }

            sb.AppendLine();
        }

        System.Console.Write(sb.ToString());
    }

    /// <inheritdoc />
    public void RenderStatistics(IDictionary<string, string> statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("### Query Statistics");
        sb.AppendLine();
        sb.AppendLine("| Statistic | Value |");
        sb.AppendLine("| --- | --- |");
        foreach (var kvp in statistics)
        {
            sb.Append("| ")
                .Append(EscapeCell(kvp.Key))
                .Append(" | ")
                .Append(EscapeCell(kvp.Value))
                .AppendLine(" |");
        }

        System.Console.Write(sb.ToString());
    }

    private static string EscapeCell(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal);
}