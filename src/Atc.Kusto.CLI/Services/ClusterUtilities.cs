namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Utility methods for cluster URL normalization and lookup.
/// </summary>
public static class ClusterUtilities
{
    /// <summary>
    /// Normalizes a cluster URL to its base authority (scheme + host).
    /// </summary>
    /// <param name="url">The cluster URL string.</param>
    /// <returns>The normalized URL, or null if invalid.</returns>
    public static string? NormalizeClusterUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        return $"{uri.Scheme}://{uri.Host}";
    }

    /// <summary>
    /// Finds a known cluster by friendly name or URL.
    /// </summary>
    /// <param name="config">The CLI configuration.</param>
    /// <param name="reference">A cluster name or URL to search for.</param>
    /// <returns>The matching cluster, or null if not found.</returns>
    public static KnownCluster? FindCluster(
        KustoCliConfig config,
        string reference)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        // Match by name (case-insensitive)
        var byName = config.Clusters.Find(x => string.Equals(x.Name, reference, StringComparison.OrdinalIgnoreCase));
        if (byName is not null)
        {
            return byName;
        }

        // Match by normalized URL
        var normalizedRef = NormalizeClusterUrl(reference);
        if (normalizedRef is null)
        {
            return null;
        }

        return config.Clusters.Find(
            x => string.Equals(
                NormalizeClusterUrl(x.Url),
                normalizedRef,
                StringComparison.OrdinalIgnoreCase));
    }
}