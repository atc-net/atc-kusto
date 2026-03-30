namespace Atc.Kusto.CLI.Commands.Settings;

/// <summary>
/// Base settings for commands that require a Kusto cluster connection.
/// Supports both explicit --cluster-url or a saved --cluster name.
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
    [Description("Saved cluster name (from 'cluster add')")]
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

        if (ClusterUrl is null && string.IsNullOrWhiteSpace(ClusterName))
        {
            return ValidationResult.Error("Either --cluster-url or --cluster is required.");
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
    /// Resolves the cluster URL from either --cluster-url or --cluster name.
    /// Must be called after validation and before using ClusterUrl.
    /// </summary>
    /// <param name="configStore">The config store to resolve cluster names.</param>
    /// <returns>True if resolved successfully, false if cluster name not found.</returns>
    public async Task<bool> ResolveClusterAsync(ICliConfigStore configStore)
    {
        ArgumentNullException.ThrowIfNull(configStore);

        if (ClusterUrl is not null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(ClusterName))
        {
            return false;
        }

        var config = await configStore.LoadAsync();
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
}