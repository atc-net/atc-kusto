namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Executes arbitrary KQL queries against a Kusto cluster.
/// </summary>
public interface IKustoQueryExecutor
{
    /// <summary>
    /// Executes a KQL query and returns the result as a data reader.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <param name="query">The KQL query text.</param>
    /// <returns>A data reader containing the query results.</returns>
    Task<System.Data.IDataReader> ExecuteQueryAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string query);
}