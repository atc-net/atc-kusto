namespace Atc.Kusto.CLI.Commands.Query;

public sealed class QueryCommand(
    ILoggerFactory loggerFactory,
    IKustoQueryExecutor queryExecutor,
    ICliConfigStore configStore)
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

        if (!await settings.ResolveClusterAsync(configStore))
        {
            AnsiConsole.MarkupLine($"[red]Cluster '{Markup.Escape(settings.ClusterName ?? string.Empty)}' not found. Use 'cluster add' to save it.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        if (!await settings.ResolveDatabaseAsync(configStore))
        {
            AnsiConsole.MarkupLine("[red]No database specified. Use --database or 'database set-default'.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        try
        {
            var queryText = await ResolveQueryTextAsync(settings);
            if (string.IsNullOrWhiteSpace(queryText))
            {
                logger.LogError("Query text is empty");
                return ConsoleExitStatusCodes.Failure;
            }

            var validationError = QueryValidator.Validate(queryText);
            if (validationError is not null)
            {
                logger.LogError("{ValidationError}", validationError);
                return ConsoleExitStatusCodes.Failure;
            }

            logger.LogInformation("Executing query against {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);
            logger.LogDebug("Query: {Query}", queryText);

            var result = await queryExecutor.ExecuteQueryAsync(
                settings.TenantId,
                settings.ClusterUrl!,
                settings.Database,
                queryText,
                settings.ShowStats);

            using var reader = result.Reader;
            var (columns, rows) = DataReaderMaterializer.Materialize(reader);

            // Try to extract statistics from subsequent result sets
            QueryStatistics? stats = null;
            if (settings.ShowStats)
            {
                stats = QueryStatisticsExtractor.Extract(reader);
            }

            var renderer = ResultRendererFactory.Create(OutputFormatParser.Parse(settings.Format));
            renderer.Render(columns, rows);

            if (stats is not null)
            {
                var statsDict = stats.ToDictionary();
                if (statsDict.Count > 0)
                {
                    renderer.RenderStatistics(statsDict);
                }
            }

            var webExplorerUrl = KustoWebExplorerUrlBuilder.Build(
                settings.ClusterUrl!,
                settings.Database,
                queryText);

            if (webExplorerUrl is not null)
            {
                renderer.RenderWebExplorerUrl(webExplorerUrl);
            }

            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Query execution failed");
            return ConsoleExitStatusCodes.Failure;
        }
    }

    private static async Task<string> ResolveQueryTextAsync(
        QueryCommandSettings settings)
    {
        if (settings.FilePath is not null)
        {
            return await ReadQueryFromFileAsync(settings.FilePath);
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

    private static async Task<string> ReadQueryFromFileAsync(
        string fileReference)
    {
        var parsed = QueryFileReferenceParser.Parse(fileReference);

        if (parsed.LineRange is null)
        {
            return (await File.ReadAllTextAsync(parsed.Path, Encoding.UTF8)).Trim();
        }

        var allLines = await File.ReadAllLinesAsync(parsed.Path, Encoding.UTF8);
        var range = parsed.LineRange.Value;

        if (range.StartLine > allLines.Length || range.EndLine > allLines.Length)
        {
            throw new InvalidOperationException(
                $"Query file range '{range.StartLine}-{range.EndLine}' is out of range for '{parsed.Path}', which has {allLines.Length} line{(allLines.Length == 1 ? string.Empty : "s")}.");
        }

        var selectedLines = allLines
            .Skip(range.StartLine - 1)
            .Take(range.LineCount);

        return string.Join(Environment.NewLine, selectedLines).Trim();
    }
}