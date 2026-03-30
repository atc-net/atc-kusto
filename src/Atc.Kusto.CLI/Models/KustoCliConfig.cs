namespace Atc.Kusto.CLI.Models;

/// <summary>
/// Persistent CLI configuration containing saved clusters and defaults.
/// </summary>
public sealed class KustoCliConfig
{
    public List<KnownCluster> Clusters { get; set; } = [];

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "JSON serialization requires string.")]
    public string? DefaultClusterUrl { get; set; }

    public Dictionary<string, string> DefaultDatabases { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
}