namespace Atc.Kusto.Providers.Internal;

public sealed class KustoClientProvider : IDisposable, IKustoClientProvider
{
    private readonly ConcurrentDictionary<ClientCacheKey, ICslQueryProvider> queryClients = new();
    private readonly ConcurrentDictionary<ClientCacheKey, ICslAdminProvider> adminClients = new();

    private readonly IOptionsMonitor<AtcKustoOptions> monitor;

    public KustoClientProvider(IOptionsMonitor<AtcKustoOptions> monitor)
    {
        this.monitor = monitor;
    }

    /// <inheritdoc />
    public ICslQueryProvider GetQueryClient(
        string? connectionName = null,
        string? databaseName = null)
        => queryClients.GetOrAdd(
            new ClientCacheKey(connectionName, databaseName),
            CreateQueryClient);

    /// <inheritdoc />
    public ICslAdminProvider GetAdminClient(
        string? connectionName = null,
        string? databaseName = null)
        => adminClients.GetOrAdd(
            new ClientCacheKey(connectionName, databaseName),
            CreateAdminClient);

    private ICslQueryProvider CreateQueryClient(ClientCacheKey clientCacheKey)
        => KustoClientFactory.CreateCslQueryProvider(
            GetConnectionString(clientCacheKey));

    private ICslAdminProvider CreateAdminClient(ClientCacheKey clientCacheKey)
        => KustoClientFactory.CreateCslAdminProvider(
            GetConnectionString(clientCacheKey));

    private KustoConnectionStringBuilder GetConnectionString(ClientCacheKey clientCacheKey)
        => monitor.Get(clientCacheKey.ConnectionName) switch
        {
            { HostAddress: { } host, DatabaseName: { } db, Credential: { } cred } =>
                new KustoConnectionStringBuilder(host.AbsoluteUri, clientCacheKey.DatabaseName ?? db)
                    .WithAadAzureTokenCredentialsAuthentication(cred),
            { HostAddress: { } host, DatabaseName: { } db } =>
                new KustoConnectionStringBuilder(host.AbsoluteUri, clientCacheKey.DatabaseName ?? db),
            { ConnectionString: { } cs, DatabaseName: { } db, Credential: { } cred }
                => new KustoConnectionStringBuilder($"{cs};Database={db}")
                    .WithAadAzureTokenCredentialsAuthentication(cred),
            { ConnectionString: { } cs, DatabaseName: { } db }
                => new KustoConnectionStringBuilder($"{cs};Database={db}"),
            { ConnectionString: { } cs, Credential: { } cred }
                => new KustoConnectionStringBuilder(cs)
                    .WithAadAzureTokenCredentialsAuthentication(cred),
            { ConnectionString: { } cs }
                => new KustoConnectionStringBuilder(cs),
            _ => throw new InvalidOperationException(
                $"Missing configuration for kusto connection: {clientCacheKey.ConnectionName}"),
        };

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var adminClient in adminClients.Values)
        {
            adminClient.Dispose();
        }

        foreach (var queryClient in queryClients.Values)
        {
            queryClient.Dispose();
        }
    }
}