namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Executes arbitrary KQL queries against a Kusto cluster.
/// </summary>
public interface IKustoQueryExecutor
{
    /// <summary>
    /// Executes a KQL query and returns the data reader and optional statistics.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <param name="query">The KQL query text.</param>
    /// <param name="includeStatistics">Whether to extract query statistics from the response.</param>
    /// <returns>A tuple of the data reader and optional statistics.</returns>
    Task<(System.Data.IDataReader Reader, QueryStatistics? Statistics)> ExecuteQueryAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string query,
        bool includeStatistics = false);
}