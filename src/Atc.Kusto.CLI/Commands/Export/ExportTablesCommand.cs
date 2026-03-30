namespace Atc.Kusto.CLI.Commands.Export;

public sealed class ExportTablesCommand(
    ILoggerFactory loggerFactory,
    IKustoSchemaExporter exporter,
    ICliConfigStore configStore)
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

    private async Task<int> ExecuteInternalAsync(
        ExportBaseCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        if (!await settings.ResolveClusterAsync(configStore))
        {
            AnsiConsole.MarkupLine($"[red]Cluster '{Markup.Escape(settings.ClusterName ?? string.Empty)}' not found. Use 'cluster add' to save it.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        if (!await settings.ResolveDatabaseAsync(configStore))
        {
            AnsiConsole.MarkupLine("[red]No database specified. Use --database or 'database set-default'.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

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