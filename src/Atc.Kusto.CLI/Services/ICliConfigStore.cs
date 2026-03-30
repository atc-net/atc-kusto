namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Loads and saves the CLI configuration.
/// </summary>
public interface ICliConfigStore
{
    /// <summary>
    /// Loads the configuration from persistent storage.
    /// </summary>
    Task<KustoCliConfig> LoadAsync();

    /// <summary>
    /// Saves the configuration to persistent storage.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    Task SaveAsync(KustoCliConfig config);
}