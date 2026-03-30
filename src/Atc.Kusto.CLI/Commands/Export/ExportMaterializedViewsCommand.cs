namespace Atc.Kusto.CLI.Commands.Export;

public sealed class ExportMaterializedViewsCommand(
    ILoggerFactory loggerFactory,
    IKustoSchemaExporter exporter)
    : AsyncCommand<ExportBaseCommandSettings>
{
    private readonly ILogger<ExportMaterializedViewsCommand> logger = loggerFactory.CreateLogger<ExportMaterializedViewsCommand>();

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ExportBaseCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(
        ExportBaseCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        try
        {
            logger.LogInformation("Exporting materialized views from {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);
            await exporter.ExportMaterializedViewsAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);
            logger.LogInformation("Materialized view export completed successfully");
            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export materialized views");
            return ConsoleExitStatusCodes.Failure;
        }
    }
}