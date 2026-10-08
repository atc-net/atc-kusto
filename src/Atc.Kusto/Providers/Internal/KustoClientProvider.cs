namespace Atc.Kusto.Providers.Internal;

public sealed class KustoClientProvider : IDisposable, IKustoClientProvider, IKustoIngestClientProvider
{
    private readonly ConcurrentDictionary<ClientCacheKey, ICslQueryProvider> queryClients = new();
    private readonly ConcurrentDictionary<ClientCacheKey, ICslAdminProvider> adminClients = new();
    private readonly ConcurrentDictionary<IngestClientCacheKey, Lazy<IKustoIngestClient>> ingestClients = new();

    private readonly IOptionsMonitor<AtcKustoOptions> monitor;
    private readonly IKustoIngestClientFactory ingestClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoClientProvider"/> class.
    /// </summary>
    /// <param name="monitor">The options monitor resolving <see cref="AtcKustoOptions"/> per named connection.</param>
    public KustoClientProvider(IOptionsMonitor<AtcKustoOptions> monitor)
        : this(monitor, new KustoIngestClientFactory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoClientProvider"/> class with a custom
    /// ingest-client factory.
    /// </summary>
    /// <remarks>
    /// Internal because <see cref="IKustoIngestClientFactory"/> is internal; used by tests to observe
    /// client creation. Dependency injection uses the public constructor.
    /// </remarks>
    /// <param name="monitor">The options monitor resolving <see cref="AtcKustoOptions"/> per named connection.</param>
    /// <param name="ingestClientFactory">The factory that creates ingest clients.</param>
    internal KustoClientProvider(
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory ingestClientFactory)
    {
        this.monitor = monitor;
        this.ingestClientFactory = ingestClientFactory;
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

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Implemented explicitly because <see cref="IKustoIngestClient"/> is internal and this class
    /// is public, so the member cannot be exposed publicly.
    /// </para>
    /// <para>
    /// The cache holds <see cref="Lazy{T}"/> values because <c>GetOrAdd</c> may run its value factory
    /// more than once under contention; a discarded ingest client would never be disposed. A failed
    /// creation is evicted rather than cached, so a corrected configuration can be retried.
    /// </para>
    /// </remarks>
    IKustoIngestClient IKustoIngestClientProvider.GetIngestClient(
        IngestionMode mode,
        string? connectionName)
    {
        var key = new IngestClientCacheKey(connectionName, mode);
        var lazyClient = ingestClients.GetOrAdd(
            key,
            static (cacheKey, provider) => new Lazy<IKustoIngestClient>(
                () => provider.CreateIngestClient(cacheKey),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        try
        {
            return lazyClient.Value;
        }
        catch
        {
            ingestClients.TryRemove(new KeyValuePair<IngestClientCacheKey, Lazy<IKustoIngestClient>>(key, lazyClient));
            throw;
        }
    }

    private IKustoIngestClient CreateIngestClient(
        IngestClientCacheKey ingestClientCacheKey)
    {
        var options = monitor.Get(ingestClientCacheKey.ConnectionName);

        if (options.HostAddress is not { } host ||
            options.Credential is not { } credential)
        {
            throw new InvalidOperationException(
                $"Ingestion requires both HostAddress and Credential for kusto connection: {ingestClientCacheKey.ConnectionName}. " +
                "ConnectionString-only or credential-less configurations are not supported for ingestion.");
        }

        return ingestClientFactory.Create(
            host,
            credential,
            options.IngestUploadContainers,
            ingestClientCacheKey.Mode);
    }

    private ICslQueryProvider CreateQueryClient(ClientCacheKey clientCacheKey)
        => KustoClientFactory.CreateCslQueryProvider(
            GetConnectionString(clientCacheKey));

    private ICslAdminProvider CreateAdminClient(ClientCacheKey clientCacheKey)
        => KustoClientFactory.CreateCslAdminProvider(
            GetConnectionString(clientCacheKey));

    private KustoConnectionStringBuilder GetConnectionString(
        ClientCacheKey clientCacheKey)
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

        foreach (var lazyClient in ingestClients.Values.Where(x => x.IsValueCreated))
        {
            lazyClient.Value.Dispose();
        }
    }
}