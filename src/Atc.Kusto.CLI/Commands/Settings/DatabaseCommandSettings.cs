namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Base settings for commands that require a specific database.
/// Resolves --database from config default when omitted.
/// </summary>
public class DatabaseCommandSettings : ClusterCommandSettings
{
    [CommandOption("--database <NAME>")]
    [Description("Database name, or omit to use default for the cluster")]
    public string Database { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        // Database can be empty here - will be resolved from config in ResolveDatabaseAsync
        return ValidationResult.Success();
    }

    /// <summary>
    /// Resolves the database from --database or the default for the current cluster.
    /// Must be called after ResolveClusterAsync.
    /// </summary>
    /// <param name="configStore">The config store to resolve default database.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if resolved successfully, false if no database could be determined.</returns>
    public async Task<bool> ResolveDatabaseAsync(
        ICliConfigStore configStore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configStore);

        if (!string.IsNullOrWhiteSpace(Database))
        {
            return true;
        }

        if (ClusterUrl is null)
        {
            return false;
        }

        var config = await configStore.LoadAsync(cancellationToken);
        var clusterKey = ClusterUtilities.NormalizeClusterUrl(ClusterUrl.AbsoluteUri);
        if (clusterKey is not null && config.DefaultDatabases.TryGetValue(clusterKey, out var defaultDb))
        {
            Database = defaultDb;
            return true;
        }

        return false;
    }
}