namespace Atc.Kusto.CLI.Commands.Cluster;

public sealed class ClusterAddCommand(
    ICliConfigStore configStore)
    : AsyncCommand<ClusterAddCommandSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ClusterAddCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ConsoleHelper.WriteHeader();

        var config = await configStore.LoadAsync();
        var normalizedUrl = ClusterUtilities.NormalizeClusterUrl(settings.Url)!;

        // Check for duplicate name
        var existingByName = config.Clusters.Find(x => string.Equals(x.Name, settings.Name, StringComparison.OrdinalIgnoreCase));
        if (existingByName is not null)
        {
            AnsiConsole.MarkupLine($"[red]A cluster named '{Markup.Escape(settings.Name)}' already exists.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        // Check for duplicate URL
        var existingByUrl = config.Clusters.Find(
            x => string.Equals(
                ClusterUtilities.NormalizeClusterUrl(x.Url),
                normalizedUrl,
                StringComparison.OrdinalIgnoreCase));

        if (existingByUrl is not null)
        {
            AnsiConsole.MarkupLine($"[red]A cluster with URL '{Markup.Escape(normalizedUrl)}' already exists as '{Markup.Escape(existingByUrl.Name)}'.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        config.Clusters.Add(new KnownCluster
        {
            Name = settings.Name,
            Url = normalizedUrl,
        });

        // Set as default if requested or if it's the first cluster
        if (settings.SetAsDefault || config.Clusters.Count == 1)
        {
            config.DefaultClusterUrl = normalizedUrl;
        }

        await configStore.SaveAsync(config);
        AnsiConsole.MarkupLine($"[green]Cluster '{Markup.Escape(settings.Name)}' added ({Markup.Escape(normalizedUrl)}).[/]");

        if (string.Equals(config.DefaultClusterUrl, normalizedUrl, StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[grey]Set as default cluster.[/]");
        }

        return ConsoleExitStatusCodes.Success;
    }
}