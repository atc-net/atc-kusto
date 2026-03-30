namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Loads and saves the CLI configuration.
/// </summary>
public interface ICliConfigStore
{
    /// <summary>
    /// Loads the configuration from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<KustoCliConfig> LoadAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the configuration to persistent storage.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync(
        KustoCliConfig config,
        CancellationToken cancellationToken = default);
}