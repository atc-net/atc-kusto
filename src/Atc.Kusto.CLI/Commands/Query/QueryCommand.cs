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

            var (columns, rows) = DataReaderMaterializer.Materialize(reader);
            var outputFormat = OutputFormatParser.Parse(settings.Format);
            var renderer = ResultRendererFactory.Create(outputFormat);
            renderer.Render(columns, rows);

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
}