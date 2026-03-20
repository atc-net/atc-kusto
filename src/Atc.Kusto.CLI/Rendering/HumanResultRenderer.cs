namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Renders results as a human-readable Spectre.Console table with ANSI colors and borders.
/// </summary>
public sealed class HumanResultRenderer : IResultRenderer
{
    /// <inheritdoc />
    public void Render(IReadOnlyList<string> columns, IReadOnlyList<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        var table = new Table();
        table.Border(TableBorder.Rounded);

        foreach (var column in columns)
        {
            table.AddColumn(new TableColumn(Markup.Escape(column)).NoWrap());
        }

        foreach (var row in rows)
        {
            var renderedCells = new string[row.Length];
            for (var i = 0; i < row.Length; i++)
            {
                renderedCells[i] = Markup.Escape(row[i]);
            }

            table.AddRow(renderedCells);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]({rows.Count} row(s))[/]");
    }
}