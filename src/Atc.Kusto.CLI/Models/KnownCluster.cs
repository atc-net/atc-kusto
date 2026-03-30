namespace Atc.Kusto.CLI.Models;

/// <summary>
/// A saved cluster entry with a friendly name and URL.
/// </summary>
public sealed class KnownCluster
{
    public string Name { get; set; } = string.Empty;

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "JSON serialization requires string.")]
    public string Url { get; set; } = string.Empty;
}