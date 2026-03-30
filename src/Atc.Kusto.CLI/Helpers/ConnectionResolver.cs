namespace Atc.Kusto.CLI.Helpers;

/// <summary>
/// Resolves cluster and database connections from settings and config,
/// writing error messages to the console on failure.
/// </summary>
public static class ConnectionResolver
{
    /// <summary>
    /// Resolves the cluster connection from settings. Writes an error and returns false on failure.
    /// </summary>
    /// <param name="settings">The cluster command settings.</param>
    /// <param name="configStore">The config store for resolving saved clusters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the cluster was resolved, false otherwise.</returns>
    public static async Task<bool> ResolveCluster(
        ClusterCommandSettings settings,
        ICliConfigStore configStore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(configStore);

        if (await settings.ResolveClusterAsync(configStore, cancellationToken))
        {
            return true;
        }

        AnsiConsole.MarkupLine(
            string.IsNullOrWhiteSpace(settings.ClusterName)
                ? "[red]No cluster specified. Use --cluster, --cluster-url, or 'cluster set-default'.[/]"
                : $"[red]Cluster '{Markup.Escape(settings.ClusterName)}' not found. Use 'cluster add' to save it.[/]");

        return false;
    }

    /// <summary>
    /// Resolves the database from settings. Writes an error and returns false on failure.
    /// </summary>
    /// <param name="settings">The database command settings.</param>
    /// <param name="configStore">The config store for resolving default databases.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the database was resolved, false otherwise.</returns>
    public static async Task<bool> ResolveDatabase(
        DatabaseCommandSettings settings,
        ICliConfigStore configStore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(configStore);

        if (await settings.ResolveDatabaseAsync(configStore, cancellationToken))
        {
            return true;
        }

        AnsiConsole.MarkupLine("[red]No database specified. Use --database or 'database set-default'.[/]");
        return false;
    }
}