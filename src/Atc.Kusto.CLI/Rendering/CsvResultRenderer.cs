namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Renders results as comma-separated values (CSV) with RFC 4180 quoting.
/// </summary>
public sealed class CsvResultRenderer : IResultRenderer
{
    /// <inheritdoc />
    public void Render(
        IReadOnlyList<string> columns,
        IReadOnlyList<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        var sb = new StringBuilder();

        // Header row
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(EscapeField(columns[i]));
        }

        sb.AppendLine();

        // Data rows
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(EscapeField(row[i]));
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
        sb.AppendLine("Statistic,Value");
        foreach (var kvp in statistics)
        {
            sb.Append(EscapeField(kvp.Key))
                .Append(',')
                .AppendLine(EscapeField(kvp.Value));
        }

        System.Console.Write(sb.ToString());
    }

    /// <inheritdoc />
    public void RenderWebExplorerUrl(Uri url)
    {
        // CSV output omits non-tabular metadata
    }

    private static string EscapeField(string value)
    {
        if (value.Contains('"', StringComparison.Ordinal) ||
            value.Contains(',', StringComparison.Ordinal) ||
            value.Contains('\n', StringComparison.Ordinal) ||
            value.Contains('\r', StringComparison.Ordinal))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}