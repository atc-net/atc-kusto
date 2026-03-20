namespace Atc.Kusto.CLI.Factories;

/// <summary>
/// Creates and caches Kusto client instances using runtime CLI arguments.
/// </summary>
public sealed class CliKustoClientFactory : ICliKustoClientFactory, IDisposable
{
    private ICslAdminProvider? adminClient;
    private ICslQueryProvider? queryClient;

    /// <inheritdoc />
    public ICslAdminProvider GetOrCreateAdminClient(
        string tenantId,
        Uri clusterUrl,
        string database)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        if (adminClient is not null)
        {
            return adminClient;
        }

        var connectionString = BuildConnectionString(tenantId, clusterUrl, database);
        adminClient = KustoClientFactory.CreateCslAdminProvider(connectionString);
        return adminClient;
    }

    /// <inheritdoc />
    public ICslQueryProvider GetOrCreateQueryClient(
        string tenantId,
        Uri clusterUrl,
        string database)
    {
        ArgumentNullException.ThrowIfNull(clusterUrl);

        if (queryClient is not null)
        {
            return queryClient;
        }

        var connectionString = BuildConnectionString(tenantId, clusterUrl, database);
        queryClient = KustoClientFactory.CreateCslQueryProvider(connectionString);
        return queryClient;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (adminClient is IDisposable adminDisposable)
        {
            adminDisposable.Dispose();
        }

        if (queryClient is IDisposable queryDisposable)
        {
            queryDisposable.Dispose();
        }
    }

    private static KustoConnectionStringBuilder BuildConnectionString(
        string tenantId,
        Uri clusterUrl,
        string database)
    {
        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions { TenantId = tenantId });

        return new KustoConnectionStringBuilder(clusterUrl.AbsoluteUri, database)
            .WithAadAzureTokenCredentialsAuthentication(credential);
    }
}