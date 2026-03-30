namespace Atc.Kusto.CLI.Commands.Cluster;

public sealed class ClusterSetDefaultCommand(
    ICliConfigStore configStore)
    : AsyncCommand<ClusterNameCommandSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ClusterNameCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ConsoleHelper.WriteHeader();

        var config = await configStore.LoadAsync();
        var cluster = ClusterUtilities.FindCluster(config, settings.Name);

        if (cluster is null)
        {
            AnsiConsole.MarkupLine($"[red]Cluster '{Markup.Escape(settings.Name)}' not found.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        config.DefaultClusterUrl = ClusterUtilities.NormalizeClusterUrl(cluster.Url) ?? cluster.Url;
        await configStore.SaveAsync(config);

        AnsiConsole.MarkupLine($"[green]Default cluster set to '{Markup.Escape(cluster.Name)}'.[/]");
        return ConsoleExitStatusCodes.Success;
    }
}