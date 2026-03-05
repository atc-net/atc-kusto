namespace Atc.Kusto.CLI.Commands.Export;

public sealed class ExportTablesCommand(
    ILoggerFactory loggerFactory,
    IKustoSchemaExporter exporter)
    : AsyncCommand<ExportBaseCommandSettings>
{
    private readonly ILogger<ExportTablesCommand> logger = loggerFactory.CreateLogger<ExportTablesCommand>();

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ExportBaseCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(ExportBaseCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        try
        {
            logger.LogInformation("Exporting tables from {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);
            await exporter.ExportTablesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);
            logger.LogInformation("Table export completed successfully");
            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export tables");
            return ConsoleExitStatusCodes.Failure;
        }
    }
}