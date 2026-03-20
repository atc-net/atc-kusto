namespace Atc.Kusto.CLI.Commands.Database;

public sealed class DatabaseShowCommand(
    ILoggerFactory loggerFactory,
    ICliKustoClientFactory clientFactory)
    : AsyncCommand<DatabaseShowCommandSettings>
{
    private readonly ILogger<DatabaseShowCommand> logger = loggerFactory.CreateLogger<DatabaseShowCommand>();

    public override Task<int> ExecuteAsync(
        CommandContext context,
        DatabaseShowCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(DatabaseShowCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        try
        {
            logger.LogInformation("Showing database {Name} on {ClusterUrl}", settings.Name, settings.ClusterUrl);

            var client = clientFactory.GetOrCreateAdminClient(
                settings.TenantId,
                settings.ClusterUrl!,
                settings.Name);

            var escapedName = settings.Name.Replace("'", "''", StringComparison.Ordinal);
            var query = $".show databases details | where DatabaseName =~ '{escapedName}'";

            using var reader = await client.ExecuteControlCommandAsync(settings.Name, query);

            var (columns, rows) = DataReaderMaterializer.Materialize(reader);
            var outputFormat = OutputFormatParser.Parse(settings.Format);
            var renderer = ResultRendererFactory.Create(outputFormat);
            renderer.Render(columns, rows);

            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show database {Name}", settings.Name);
            return ConsoleExitStatusCodes.Failure;
        }
    }
}