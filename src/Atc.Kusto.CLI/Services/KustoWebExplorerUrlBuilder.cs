namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Builds deep-link URLs to the Azure Data Explorer web UI with embedded query text.
/// </summary>
public static class KustoWebExplorerUrlBuilder
{
    private const int MaxUrlLength = 8000;

    private static readonly (string Suffix, string ExplorerBase)[] CloudMappings =
    [
        (".kusto.windows.net", "https://dataexplorer.azure.com"),
        (".kustodev.windows.net", "https://dataexplorer.azure.com"),
        (".kustomfa.windows.net", "https://dataexplorer.azure.com"),
        (".kusto.data.microsoft.com", "https://dataexplorer.azure.com"),
        (".kusto.fabric.microsoft.com", "https://dataexplorer.azure.com"),
        (".kusto.azuresynapse.net", "https://dataexplorer.azure.com"),
        (".kusto.usgovcloudapi.net", "https://dataexplorer.azure.us"),
        (".kustomfa.usgovcloudapi.net", "https://dataexplorer.azure.us"),
        (".kusto.chinacloudapi.cn", "https://dataexplorer.azure.cn"),
        (".kustomfa.chinacloudapi.cn", "https://dataexplorer.azure.cn"),
        (".kusto.azuresynapse.azure.cn", "https://dataexplorer.azure.cn"),
    ];

    /// <summary>
    /// Builds a web explorer URL for the given cluster, database, and query.
    /// Returns null if the cluster is not a recognized Azure cloud endpoint
    /// or if the resulting URL exceeds the maximum length.
    /// </summary>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <param name="query">The KQL query text.</param>
    /// <returns>The web explorer URI, or null if it cannot be built.</returns>
    public static Uri? Build(
        Uri clusterUrl,
        string database,
        string query)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(query);

        var explorerBase = ResolveExplorerBase(clusterUrl.Host);
        if (explorerBase is null)
        {
            return null;
        }

        var encodedQuery = Uri.EscapeDataString(
            Convert.ToBase64String(
                CompressQuery(query.Trim())));

        var url = $"{explorerBase}/clusters/{clusterUrl.Host}/databases/{Uri.EscapeDataString(database)}?query={encodedQuery}";

        return url.Length <= MaxUrlLength ? new Uri(url) : null;
    }

    private static string? ResolveExplorerBase(string host)
    {
        foreach (var (suffix, explorerBase) in CloudMappings)
        {
            if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return explorerBase;
            }
        }

        return null;
    }

    private static byte[] CompressQuery(string query)
    {
        var queryBytes = Encoding.UTF8.GetBytes(query);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(queryBytes, 0, queryBytes.Length);
        }

        return output.ToArray();
    }
}