namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Executes arbitrary KQL queries using the CLI Kusto client factory.
/// </summary>
public sealed class KustoQueryExecutor(
    ICliKustoClientFactory clientFactory)
    : IKustoQueryExecutor
{
    /// <inheritdoc />
    public Task<System.Data.IDataReader> ExecuteQueryAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string query)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        var client = clientFactory.GetOrCreateQueryClient(tenantId, clusterUrl, database);
        return client.ExecuteQueryAsync(database, query, new ClientRequestProperties());
    }
}