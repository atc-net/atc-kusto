namespace Atc.Kusto.CLI.Commands.Table;

public sealed class TableShowCommand(
    ILoggerFactory loggerFactory,
    ICliKustoClientFactory clientFactory)
    : AsyncCommand<TableShowCommandSettings>
{
    private readonly ILogger<TableShowCommand> logger = loggerFactory.CreateLogger<TableShowCommand>();

    public override Task<int> ExecuteAsync(
        CommandContext context,
        TableShowCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(TableShowCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        try
        {
            logger.LogInformation("Showing table {Name} in {ClusterUrl}/{Database}", settings.Name, settings.ClusterUrl, settings.Database);

            var client = clientFactory.GetOrCreateAdminClient(
                settings.TenantId,
                settings.ClusterUrl!,
                settings.Database);

            var escapedName = settings.Name.Replace("'", "''", StringComparison.Ordinal);
            var query = $".show table ['{escapedName}'] cslschema";

            using var reader = await client.ExecuteControlCommandAsync(settings.Database, query);

            var (columns, rows) = DataReaderMaterializer.Materialize(reader);
            var outputFormat = OutputFormatParser.Parse(settings.Format);
            var renderer = ResultRendererFactory.Create(outputFormat);
            renderer.Render(columns, rows);

            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show table {Name}", settings.Name);
            return ConsoleExitStatusCodes.Failure;
        }
    }
}