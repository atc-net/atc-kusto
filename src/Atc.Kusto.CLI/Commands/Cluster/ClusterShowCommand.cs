namespace Atc.Kusto.CLI.Commands.Cluster;

public sealed class ClusterShowCommand(
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

        var config = await configStore.LoadAsync(cancellationToken);
        var cluster = ClusterUtilities.FindCluster(config, settings.Name);

        if (cluster is null)
        {
            AnsiConsole.MarkupLine($"[red]Cluster '{Markup.Escape(settings.Name)}' not found.[/]");
            return ConsoleExitStatusCodes.Failure;
        }

        var normalizedUrl = ClusterUtilities.NormalizeClusterUrl(cluster.Url) ?? cluster.Url;
        var isDefault = string.Equals(
            normalizedUrl,
            ClusterUtilities.NormalizeClusterUrl(config.DefaultClusterUrl ?? string.Empty),
            StringComparison.OrdinalIgnoreCase);

        config.DefaultDatabases.TryGetValue(normalizedUrl, out var defaultDb);

        var table = new Spectre.Console.Table();
        table.Border(TableBorder.Simple);
        table.HideHeaders();
        table.AddColumn("Property");
        table.AddColumn("Value");
        table.AddRow("Name", Markup.Escape(cluster.Name));
        table.AddRow("URL", Markup.Escape(cluster.Url));
        table.AddRow("Default", isDefault ? "[green]Yes[/]" : "No");

        if (defaultDb is not null)
        {
            table.AddRow("Default Database", Markup.Escape(defaultDb));
        }

        AnsiConsole.Write(table);
        return ConsoleExitStatusCodes.Success;
    }
}