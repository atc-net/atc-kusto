namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Renders results as a human-readable Spectre.Console table with ANSI colors and borders.
/// </summary>
public sealed class HumanResultRenderer : IResultRenderer
{
    /// <inheritdoc />
    public void Render(
        IReadOnlyList<string> columns,
        IReadOnlyList<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        // Detect numeric columns from raw data before formatting
        var rightAligned = DetectNumericColumns(columns.Count, rows);

        var table = new Table();
        table.Border(TableBorder.Rounded);

        for (var i = 0; i < columns.Count; i++)
        {
            var col = new TableColumn(Markup.Escape(columns[i])).NoWrap();
            if (rightAligned[i])
            {
                col.RightAligned();
            }

            table.AddColumn(col);
        }

        foreach (var row in rows)
        {
            var renderedCells = new string[row.Length];
            for (var i = 0; i < row.Length; i++)
            {
                renderedCells[i] = Markup.Escape(DisplayValueFormatter.FormatCellValue(row[i]));
            }

            table.AddRow(renderedCells);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]({rows.Count} row(s))[/]");
    }

    /// <inheritdoc />
    public void RenderStatistics(IDictionary<string, string> statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Query Statistics[/]");
        var statsTable = new Table();
        statsTable.Border(TableBorder.Simple);
        statsTable.AddColumn(new TableColumn("Statistic").NoWrap());
        statsTable.AddColumn(new TableColumn("Value").NoWrap());
        foreach (var kvp in statistics)
        {
            statsTable.AddRow(Markup.Escape(kvp.Key), Markup.Escape(kvp.Value));
        }

        AnsiConsole.Write(statsTable);
    }

    /// <inheritdoc />
    public void RenderWebExplorerUrl(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[link={Markup.Escape(url.AbsoluteUri)}]Open in Web Explorer[/]");
    }

    private static bool[] DetectNumericColumns(
        int columnCount,
        IReadOnlyList<string[]> rows)
    {
        var result = new bool[columnCount];
        if (rows.Count == 0)
        {
            return result;
        }

        for (var i = 0; i < columnCount; i++)
        {
            var allNumeric = true;
            foreach (var row in rows)
            {
                if (i >= row.Length || string.IsNullOrEmpty(row[i]))
                {
                    continue;
                }

                if (!DisplayValueFormatter.IsNumericValue(row[i]))
                {
                    allNumeric = false;
                    break;
                }
            }

            result[i] = allNumeric;
        }

        return result;
    }
}