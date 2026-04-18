namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Utility methods for cluster URL normalization and lookup.
/// </summary>
public static class ClusterUtilities
{
    // Fixed Kusto hostnames that front ADX as a proxy (ADE / Azure Monitor / Aria /
    // security platform). Unlike classic ADX clusters (e.g., *.kusto.windows.net),
    // a request to these hosts may carry a workspace- or resource-specific path that
    // MUST be preserved end-to-end; collapsing to the bare hostname causes the proxy
    // to reject the request (e.g., InvalidClusterHostName from prod-adxproxy) and
    // makes it impossible to distinguish two workspaces on the same host.
    //
    // Source: the AllowedKustoHostnames entries in the public well-known Kusto
    // endpoints list shipped with the official Azure Data Explorer SDKs.
    private static readonly HashSet<string> ProxyHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        // Public cloud (login.microsoftonline.com)
        "ade.applicationinsights.io",
        "ade.loganalytics.io",
        "adx.aimon.applicationinsights.azure.com",
        "adx.applicationinsights.azure.com",
        "adx.int.applicationinsights.azure.com",
        "adx.int.loganalytics.azure.com",
        "adx.int.monitor.azure.com",
        "adx.loganalytics.azure.com",
        "adx.monitor.azure.com",
        "kusto.aria.microsoft.com",
        "eu.kusto.aria.microsoft.com",
        "api.securityplatform.microsoft.com",

        // US Government (Fairfax)
        "adx.applicationinsights.azure.us",
        "adx.loganalytics.azure.us",
        "adx.monitor.azure.us",

        // China (Mooncake)
        "adx.applicationinsights.azure.cn",
        "adx.loganalytics.azure.cn",
        "adx.monitor.azure.cn",

        // US Nat (EagleX)
        "adx.applicationinsights.azure.eaglex.ic.gov",
        "adx.loganalytics.azure.eaglex.ic.gov",
        "adx.monitor.azure.eaglex.ic.gov",

        // US Sec (scloud)
        "adx.applicationinsights.azure.microsoft.scloud",
        "adx.loganalytics.azure.microsoft.scloud",
        "adx.monitor.azure.microsoft.scloud",

        // France sovereign (Bleu)
        "adx.applicationinsights.azure.fr",
        "adx.loganalytics.azure.fr",
        "adx.monitor.azure.fr",

        // Germany sovereign (Delos)
        "adx.applicationinsights.azure.de",
        "adx.loganalytics.azure.de",
        "adx.monitor.azure.de",

        // Singapore sovereign (GovSG)
        "adx.applicationinsights.azure.sg",
        "adx.loganalytics.azure.sg",
        "adx.monitor.azure.sg",
    };

    /// <summary>
    /// Normalizes a cluster URL for consistent storage and lookup.
    /// Classic ADX clusters collapse to scheme+host; known proxy hosts
    /// (ADE/ADX/Azure Monitor/etc.) preserve their resource path.
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

        if (IsProxyHost(uri.Host) && uri.AbsolutePath.Length > 1)
        {
            var builder = new UriBuilder(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty,
            };

            return builder.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        }

        return $"{uri.Scheme}://{uri.Host}";
    }

    /// <summary>
    /// Returns true if the given host is a known Kusto proxy endpoint
    /// (ADE, ADX proxy, Azure Monitor, Aria, security platform).
    /// </summary>
    /// <param name="host">The host name to check.</param>
    public static bool IsProxyHost(string host) => ProxyHosts.Contains(host);

    /// <summary>
    /// Returns a config with every URL-bearing field re-normalized so in-memory
    /// state stays consistent regardless of what was written to disk by older
    /// versions or by hand. The same instance is returned; fields are mutated.
    /// </summary>
    /// <param name="config">The CLI configuration to normalize.</param>
    /// <returns>The same instance, with normalized URLs.</returns>
    public static KustoCliConfig NormalizeConfig(KustoCliConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!string.IsNullOrWhiteSpace(config.DefaultClusterUrl))
        {
            var normalized = NormalizeClusterUrl(config.DefaultClusterUrl);
            if (normalized is not null)
            {
                config.DefaultClusterUrl = normalized;
            }
        }

        foreach (var cluster in config.Clusters)
        {
            var normalized = NormalizeClusterUrl(cluster.Url);
            if (normalized is not null)
            {
                cluster.Url = normalized;
            }
        }

        if (config.DefaultDatabases.Count > 0)
        {
            var rebuilt = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in config.DefaultDatabases)
            {
                var key = NormalizeClusterUrl(pair.Key) ?? pair.Key;
                rebuilt[key] = pair.Value;
            }

            config.DefaultDatabases = rebuilt;
        }

        return config;
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

        var byName = config.Clusters.Find(x => string.Equals(x.Name, reference, StringComparison.OrdinalIgnoreCase));
        if (byName is not null)
        {
            return byName;
        }

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