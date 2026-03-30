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
            logger.LogInformation("Exporting full schema from {ClusterUrl}/{Database}", settings.ClusterUrl, settings.Database);

            await exporter.ExportTablesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);
            await exporter.ExportFunctionsAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);
            await exporter.ExportMaterializedViewsAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);
            await exporter.ExportExternalTablesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);
            await exporter.ExportPoliciesAsync(settings.TenantId, settings.ClusterUrl!, settings.Database, settings.OutputDir);

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