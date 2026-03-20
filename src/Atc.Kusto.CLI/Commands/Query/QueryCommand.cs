namespace Atc.Kusto.CLI.Commands.Query;

public sealed class QueryCommand(
    ILoggerFactory loggerFactory,
    IKustoQueryExecutor queryExecutor)
    : AsyncCommand<QueryCommandSettings>
{
    private readonly ILogger<QueryCommand> logger = loggerFactory.CreateLogger<QueryCommand>();

    public override Task<int> ExecuteAsync(
        CommandContext context,
        QueryCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(QueryCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        try
        {
            var queryText = await ResolveQueryTextAsync(settings);
            if (string.IsNullOrWhiteSpace(queryText))
            {
                logger.LogError("Query text is empty");
                return ConsoleExitStatusCodes.Failure;
            }

            logger.LogInformation("Executing query against {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);
            logger.LogDebug("Query: {Query}", queryText);

            using var reader = await queryExecutor.ExecuteQueryAsync(
                settings.TenantId,
                settings.ClusterUrl!,
                settings.Database,
                queryText);

            RenderResults(reader, settings.Format);

            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Query execution failed");
            return ConsoleExitStatusCodes.Failure;
        }
    }

    private static async Task<string> ResolveQueryTextAsync(QueryCommandSettings settings)
    {
        if (settings.FilePath is not null)
        {
            return await File.ReadAllTextAsync(settings.FilePath, Encoding.UTF8);
        }

        if (settings.Query is not null)
        {
            if (settings.Query == "-")
            {
                return await System.Console.In.ReadToEndAsync();
            }

            return settings.Query;
        }

        if (System.Console.IsInputRedirected)
        {
            return await System.Console.In.ReadToEndAsync();
        }

        return string.Empty;
    }

    private static void RenderResults(System.Data.IDataReader reader, string format)
    {
        var columns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        var rows = new List<string[]>();
        while (reader.Read())
        {
            var row = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
            }

            rows.Add(row);
        }

        switch (format)
        {
            case "json":
                RenderJson(columns, rows);
                break;
            case "markdown" or "md":
                RenderMarkdown(columns, rows);
                break;
            default:
                RenderHuman(columns, rows);
                break;
        }
    }

    private static void RenderHuman(List<string> columns, List<string[]> rows)
    {
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

    private static void RenderJson(List<string> columns, List<string[]> rows)
    {
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

    private static void RenderMarkdown(List<string> columns, List<string[]> rows)
    {
        var sb = new StringBuilder();

        // Header row
        sb.Append('|');
        foreach (var column in columns)
        {
            sb.Append(' ').Append(EscapeMarkdownCell(column)).Append(" |");
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
                sb.Append(' ').Append(EscapeMarkdownCell(cell)).Append(" |");
            }

            sb.AppendLine();
        }

        System.Console.Write(sb.ToString());
    }

    private static string EscapeMarkdownCell(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal);
}