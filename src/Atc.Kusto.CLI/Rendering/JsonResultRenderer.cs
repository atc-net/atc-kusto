namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Renders results as a JSON array for scripting and automation.
/// </summary>
public sealed class JsonResultRenderer : IResultRenderer
{
    /// <inheritdoc />
    public void Render(IReadOnlyList<string> columns, IReadOnlyList<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        var sb = new StringBuilder();
        sb.AppendLine("[");

        for (var r = 0; r < rows.Count; r++)
        {
            sb.AppendLine("  {");
            for (var c = 0; c < columns.Count; c++)
            {
                var escapedValue = rows[r][c]
                    .Replace("\\", "\\\\", StringComparison.Ordinal)
                    .Replace("\"", "\\\"", StringComparison.Ordinal);
                var separator = c < columns.Count - 1 ? "," : string.Empty;
                sb.Append("    \"").Append(columns[c]).Append("\": \"").Append(escapedValue).Append('"').AppendLine(separator);
            }

            var rowSeparator = r < rows.Count - 1 ? "," : string.Empty;
            sb.Append("  }").AppendLine(rowSeparator);
        }

        sb.Append(']');
        System.Console.WriteLine(sb.ToString());
    }

    /// <inheritdoc />
    public void RenderStatistics(IDictionary<string, string> statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("  \"statistics\": {");

        var entries = statistics.ToList();
        for (var i = 0; i < entries.Count; i++)
        {
            var separator = i < entries.Count - 1 ? "," : string.Empty;
            var escapedValue = entries[i].Value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
            sb.Append("    \"").Append(entries[i].Key).Append("\": \"").Append(escapedValue).Append('"').AppendLine(separator);
        }

        sb.AppendLine("  }");
        sb.Append('}');
        System.Console.WriteLine(sb.ToString());
    }
}