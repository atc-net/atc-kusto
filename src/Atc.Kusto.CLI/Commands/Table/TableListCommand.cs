namespace Atc.Kusto.CLI.Commands.Table;

public sealed class TableListCommand(
    ILoggerFactory loggerFactory,
    ICliKustoClientFactory clientFactory,
    ICliConfigStore configStore)
    : AsyncCommand<TableListCommandSettings>
{
    private readonly ILogger<TableListCommand> logger = loggerFactory.CreateLogger<TableListCommand>();

    public override Task<int> ExecuteAsync(
        CommandContext context,
        TableListCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        TableListCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        if (!await settings.ResolveClusterAsync(configStore, cancellationToken))
        {
            AnsiConsole.MarkupLine($"[red]Cluster '{Markup.Escape(settings.ClusterName ?? string.Empty)}' not found. Use 'cluster add' to save it.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        if (!await settings.ResolveDatabaseAsync(configStore, cancellationToken))
        {
            AnsiConsole.MarkupLine("[red]No database specified. Use --database or 'database set-default'.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        try
        {
            logger.LogInformation("Listing tables in {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);

            var client = clientFactory.GetOrCreateAdminClient(
                settings.TenantId,
                settings.ClusterUrl!,
                settings.Database);

            var query = ".show tables | project TableName";

            if (settings.Filter is not null)
            {
                query = query + " " + FilterBuilder.Build("TableName", settings.Filter);
            }

            if (settings.Take is not null)
            {
                query = query + " | take " + settings.Take.Value.ToString(CultureInfo.InvariantCulture);
            }

            using var reader = await client.ExecuteControlCommandAsync(settings.Database, query);

            var (columns, rows) = DataReaderMaterializer.Materialize(reader);
            var outputFormat = OutputFormatParser.Parse(settings.Format);
            var renderer = ResultRendererFactory.Create(outputFormat);
            renderer.Render(columns, rows);

            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list tables");
            return ConsoleExitStatusCodes.Failure;
        }
    }
}