namespace Atc.Kusto.CLI.Commands.Database;

public sealed class DatabaseListCommand(
    ILoggerFactory loggerFactory,
    ICliKustoClientFactory clientFactory,
    ICliConfigStore configStore)
    : AsyncCommand<DatabaseListCommandSettings>
{
    private readonly ILogger<DatabaseListCommand> logger = loggerFactory.CreateLogger<DatabaseListCommand>();

    public override Task<int> ExecuteAsync(
        CommandContext context,
        DatabaseListCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(
        DatabaseListCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        if (!await settings.ResolveClusterAsync(configStore))
        {
            AnsiConsole.MarkupLine($"[red]Cluster '{Markup.Escape(settings.ClusterName ?? string.Empty)}' not found. Use 'cluster add' to save it.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        try
        {
            logger.LogInformation("Listing databases on {ClusterUrl}", settings.ClusterUrl);

            var client = clientFactory.GetOrCreateAdminClient(
                settings.TenantId,
                settings.ClusterUrl!,
                "NetDefaultDB");

            var query = ".show databases | project DatabaseName";

            if (settings.Filter is not null)
            {
                query = query + " " + Helpers.FilterBuilder.Build("DatabaseName", settings.Filter);
            }

            if (settings.Take is not null)
            {
                query = query + " | take " + settings.Take.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            using var reader = await client.ExecuteControlCommandAsync(string.Empty, query);

            var (columns, rows) = DataReaderMaterializer.Materialize(reader);
            var outputFormat = OutputFormatParser.Parse(settings.Format);
            var renderer = ResultRendererFactory.Create(outputFormat);
            renderer.Render(columns, rows);

            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list databases");
            return ConsoleExitStatusCodes.Failure;
        }
    }
}