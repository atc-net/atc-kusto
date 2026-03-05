namespace Atc.Kusto.CLI.Commands.Export;

public sealed class ExportExternalTablesCommand(
    ILoggerFactory loggerFactory,
    IKustoSchemaExporter exporter)
    : AsyncCommand<ExportBaseCommandSettings>
{
    private readonly ILogger<ExportExternalTablesCommand> logger = loggerFactory.CreateLogger<ExportExternalTablesCommand>();

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
            logger.LogInformation("Exporting external tables from {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);
            await exporter.ExportExternalTablesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);
            logger.LogInformation("External table export completed successfully");
            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export external tables");
            return ConsoleExitStatusCodes.Failure;
        }
    }
}