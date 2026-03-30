namespace Atc.Kusto.CLI.Commands.Cluster;

public sealed class ClusterRemoveCommand(
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

        var normalizedUrl = ClusterUtilities.NormalizeClusterUrl(cluster.Url) ?? cluster.Url;

        config.Clusters.Remove(cluster);
        config.DefaultDatabases.Remove(normalizedUrl);

        if (string.Equals(config.DefaultClusterUrl, normalizedUrl, StringComparison.OrdinalIgnoreCase))
        {
            config.DefaultClusterUrl = null;
        }

        await configStore.SaveAsync(config);
        AnsiConsole.MarkupLine($"[green]Cluster '{Markup.Escape(cluster.Name)}' removed.[/]");
        return ConsoleExitStatusCodes.Success;
    }
}