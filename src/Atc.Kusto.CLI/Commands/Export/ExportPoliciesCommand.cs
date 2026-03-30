namespace Atc.Kusto.CLI.Commands.Export;

public sealed class ExportPoliciesCommand(
    ILoggerFactory loggerFactory,
    IKustoSchemaExporter exporter)
    : AsyncCommand<ExportBaseCommandSettings>
{
    private readonly ILogger<ExportPoliciesCommand> logger = loggerFactory.CreateLogger<ExportPoliciesCommand>();

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