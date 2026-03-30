namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Base settings for commands that require a Kusto cluster connection.
/// Supports explicit --cluster-url, saved --cluster name, or default from config.
/// </summary>
public class ClusterCommandSettings : BaseCommandSettings
{
    [CommandOption("--tenant-id <GUID>")]
    [Description("Azure AD tenant ID")]
    public string TenantId { get; init; } = string.Empty;

    [CommandOption("--cluster-url <URL>")]
    [Description("Kusto cluster URL (e.g. https://mycluster.kusto.windows.net)")]
    public Uri? ClusterUrl { get; set; }

    [CommandOption("--cluster <NAME>")]
    [Description("Saved cluster name (from 'cluster add'), or omit to use default")]
    public string? ClusterName { get; init; }

    public override ValidationResult Validate()
    {
        var baseResult = base.Validate();
        if (!baseResult.Successful)
        {
            return baseResult;
        }

        if (string.IsNullOrWhiteSpace(TenantId))
        {
            return ValidationResult.Error("--tenant-id is required.");
        }

        if (ClusterUrl is not null && !string.IsNullOrWhiteSpace(ClusterName))
        {
            return ValidationResult.Error("Specify either --cluster-url or --cluster, not both.");
        }

        if (ClusterUrl is not null && ClusterUrl.Scheme is not "https" and not "http")
        {
            return ValidationResult.Error("--cluster-url must be a valid HTTP(S) URL.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Resolves the cluster URL from --cluster-url, --cluster name, or default config.
    /// Must be called after validation and before using ClusterUrl.
    /// </summary>
    /// <param name="configStore">The config store to resolve cluster names.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if resolved successfully, false if no cluster could be determined.</returns>
    public async Task<bool> ResolveClusterAsync(
        ICliConfigStore configStore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configStore);

        if (ClusterUrl is not null)
        {
            return true;
        }

        var config = await configStore.LoadAsync(cancellationToken);

        // Try to resolve by name
        if (!string.IsNullOrWhiteSpace(ClusterName))
        {
            var cluster = ClusterUtilities.FindCluster(config, ClusterName);
            if (cluster is null)
            {
                return false;
            }

            var normalized = ClusterUtilities.NormalizeClusterUrl(cluster.Url);
            if (normalized is null)
            {
                return false;
            }

            ClusterUrl = new Uri(normalized);
            return true;
        }

        // Fall back to default cluster from config
        if (!string.IsNullOrWhiteSpace(config.DefaultClusterUrl))
        {
            ClusterUrl = new Uri(config.DefaultClusterUrl);
            return true;
        }

        return false;
    }
}