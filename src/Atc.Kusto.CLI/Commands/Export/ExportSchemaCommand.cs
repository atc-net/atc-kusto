namespace Atc.Kusto.CLI.Commands.Export;

public sealed class ExportSchemaCommand(
    ILoggerFactory loggerFactory,
    IKustoSchemaExporter exporter,
    ICliConfigStore configStore)
    : AsyncCommand<ExportBaseCommandSettings>
{
    private readonly ILogger<ExportSchemaCommand> logger = loggerFactory.CreateLogger<ExportSchemaCommand>();

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ExportBaseCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        ExportBaseCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        if (!await ConnectionResolver.ResolveCluster(settings, configStore, cancellationToken))
        {
            return ConsoleExitStatusCodes.Failure;
        }

        if (!await ConnectionResolver.ResolveDatabase(settings, configStore, cancellationToken))
        {
            return ConsoleExitStatusCodes.Failure;
        }

        try
        {
            logger.LogInformation("Exporting full schema from {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);

            await exporter.ExportTablesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir, cancellationToken);
            await exporter.ExportFunctionsAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir, cancellationToken);
            await exporter.ExportMaterializedViewsAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir, cancellationToken);
            await exporter.ExportExternalTablesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir, cancellationToken);
            await exporter.ExportPoliciesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir, cancellationToken);

            logger.LogInformation("Schema export completed successfully");
            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export schema");
            return ConsoleExitStatusCodes.Failure;
        }
    }
}