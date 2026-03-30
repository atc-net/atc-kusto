namespace Atc.Kusto.CLI.Commands.Database;

public sealed class DatabaseSetDefaultCommand(
    ICliConfigStore configStore)
    : AsyncCommand<DatabaseSetDefaultCommandSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DatabaseSetDefaultCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ConsoleHelper.WriteHeader();

        if (!await settings.ResolveClusterAsync(configStore, cancellationToken))
        {
            AnsiConsole.MarkupLine("[red]No cluster specified. Use --cluster, --cluster-url, or set a default with 'cluster set-default'.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        var config = await configStore.LoadAsync(cancellationToken);
        var clusterKey = ClusterUtilities.NormalizeClusterUrl(settings.ClusterUrl!.AbsoluteUri);
        if (clusterKey is null)
        {
            AnsiConsole.MarkupLine("[red]Invalid cluster URL.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        config.DefaultDatabases[clusterKey] = settings.DatabaseName;
        await configStore.SaveAsync(config, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Default database for '{Markup.Escape(clusterKey)}' set to '{Markup.Escape(settings.DatabaseName)}'.[/]");
        return ConsoleExitStatusCodes.Success;
    }
}