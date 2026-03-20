namespace Atc.Kusto.CLI.Factories;

/// <summary>
/// Provides Kusto client instances for CLI commands using runtime arguments.
/// </summary>
public interface ICliKustoClientFactory
{
    /// <summary>
    /// Gets or creates a cached <see cref="ICslAdminProvider"/> for executing control commands.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <returns>A configured admin client.</returns>
    ICslAdminProvider GetOrCreateAdminClient(
        string tenantId,
        Uri clusterUrl,
        string database);

    /// <summary>
    /// Gets or creates a cached <see cref="ICslQueryProvider"/> for executing queries.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <returns>A configured query client.</returns>
    ICslQueryProvider GetOrCreateQueryClient(
        string tenantId,
        Uri clusterUrl,
        string database);
}