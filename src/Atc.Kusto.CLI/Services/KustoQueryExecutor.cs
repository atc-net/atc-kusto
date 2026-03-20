namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Executes arbitrary KQL queries using the CLI Kusto client factory.
/// </summary>
public sealed class KustoQueryExecutor(
    ICliKustoClientFactory clientFactory)
    : IKustoQueryExecutor
{
    /// <inheritdoc />
    public async Task<(System.Data.IDataReader Reader, QueryStatistics? Statistics)> ExecuteQueryAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string query,
        bool includeStatistics = false)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        var client = clientFactory.GetOrCreateQueryClient(tenantId, clusterUrl, database);
        var properties = new ClientRequestProperties();

        if (includeStatistics)
        {
            properties.SetOption("deferpartialqueryfailures", true);
        }

        var reader = await client.ExecuteQueryAsync(database, query, properties);
        return (reader, null);
    }
}