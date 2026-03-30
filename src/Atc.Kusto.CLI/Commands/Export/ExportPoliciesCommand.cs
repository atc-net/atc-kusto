namespace Atc.Kusto.CLI.Commands.Export;

public sealed class ExportPoliciesCommand(
    ILoggerFactory loggerFactory,
    IKustoSchemaExporter exporter,
    ICliConfigStore configStore)
    : AsyncCommand<ExportBaseCommandSettings>
{
    private readonly ILogger<ExportPoliciesCommand> logger = loggerFactory.CreateLogger<ExportPoliciesCommand>();

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
            logger.LogInformation("Exporting policies from {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);
            await exporter.ExportPoliciesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);
            logger.LogInformation("Policy export completed successfully");
            return ConsoleExitStatusCodes.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export policies");
            return ConsoleExitStatusCodes.Failure;
        }
    }
}