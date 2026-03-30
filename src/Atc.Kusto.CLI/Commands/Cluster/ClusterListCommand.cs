namespace Atc.Kusto.CLI.Commands.Cluster;

public sealed class ClusterListCommand(
    ICliConfigStore configStore)
    : AsyncCommand
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        var config = await configStore.LoadAsync();

        if (config.Clusters.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No clusters saved. Use 'cluster add' to add one.[/]");
            return ConsoleExitStatusCodes.Success;
        }

        var table = new Spectre.Console.Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("Name").NoWrap());
        table.AddColumn(new TableColumn("URL").NoWrap());
        table.AddColumn(new TableColumn("Default").NoWrap());

        foreach (var cluster in config.Clusters)
        {
            var isDefault = string.Equals(
                ClusterUtilities.NormalizeClusterUrl(cluster.Url),
                ClusterUtilities.NormalizeClusterUrl(config.DefaultClusterUrl ?? string.Empty),
                StringComparison.OrdinalIgnoreCase);

            table.AddRow(
                Markup.Escape(cluster.Name),
                Markup.Escape(cluster.Url),
                isDefault ? "[green]*[/]" : string.Empty);
        }

        AnsiConsole.Write(table);
        return ConsoleExitStatusCodes.Success;
    }
}