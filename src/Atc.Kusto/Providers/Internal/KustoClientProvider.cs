namespace Atc.Kusto.Providers.Internal;

public sealed class KustoClientProvider : IDisposable, IKustoClientProvider, IKustoIngestClientProvider
{
    private readonly ConcurrentDictionary<ClientCacheKey, Lazy<ICslQueryProvider>> queryClients = new();
    private readonly ConcurrentDictionary<ClientCacheKey, Lazy<ICslAdminProvider>> adminClients = new();
    private readonly ConcurrentDictionary<IngestClientCacheKey, Lazy<IKustoIngestClient>> ingestClients = new();

    private readonly IOptionsMonitor<AtcKustoOptions> monitor;
    private readonly IKustoIngestClientFactory ingestClientFactory;
    private readonly IKustoDataClientFactory dataClientFactory;

    private int disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoClientProvider"/> class.
    /// </summary>
    /// <param name="monitor">The options monitor resolving <see cref="AtcKustoOptions"/> per named connection.</param>
    public KustoClientProvider(IOptionsMonitor<AtcKustoOptions> monitor)
        : this(monitor, new KustoIngestClientFactory(), new KustoDataClientFactory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoClientProvider"/> class with custom
    /// client factories.
    /// </summary>
    /// <remarks>
    /// Internal because the factory interfaces are internal; used by tests to observe client creation.
    /// Dependency injection uses the public constructor.
    /// </remarks>
    /// <param name="monitor">The options monitor resolving <see cref="AtcKustoOptions"/> per named connection.</param>
    /// <param name="ingestClientFactory">The factory that creates ingest clients.</param>
    /// <param name="dataClientFactory">The factory that creates query and admin clients.</param>
    internal KustoClientProvider(
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory ingestClientFactory,
        IKustoDataClientFactory dataClientFactory)
    {
        this.monitor = monitor;
        this.ingestClientFactory = ingestClientFactory;
        this.dataClientFactory = dataClientFactory;
    }

    /// <inheritdoc />
    public ICslQueryProvider GetQueryClient(
        string? connectionName = null,
        string? databaseName = null)
        => GetOrCreate(
            queryClients,
            new ClientCacheKey(connectionName, databaseName),
            CreateQueryClient);

    /// <inheritdoc />
    public ICslAdminProvider GetAdminClient(
        string? connectionName = null,
        string? databaseName = null)
        => GetOrCreate(
            adminClients,
            new ClientCacheKey(connectionName, databaseName),
            CreateAdminClient);

    /// <inheritdoc />
    /// <remarks>
    /// Implemented explicitly because <see cref="IKustoIngestClient"/> is internal and this class
    /// is public, so the member cannot be exposed publicly.
    /// </remarks>
    IKustoIngestClient IKustoIngestClientProvider.GetIngestClient(
        IngestionMode mode,
        string? connectionName)
        => GetOrCreate(
            ingestClients,
            new IngestClientCacheKey(connectionName, mode),
            CreateIngestClient);

    /// <summary>
    /// Returns the cached client for <paramref name="key"/>, creating it on first use.
    /// </summary>
    /// <remarks>
    /// The cache holds <see cref="Lazy{T}"/> values because <c>GetOrAdd</c> may run its value factory
    /// more than once under contention; a client created by a losing call would be dropped without
    /// being disposed. With <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>, concurrent first
    /// calls create exactly one client. A failed creation is evicted rather than cached, so a corrected
    /// configuration can be retried.
    /// </remarks>
    /// <typeparam name="TKey">The cache key type.</typeparam>
    /// <typeparam name="TClient">The client type.</typeparam>
    /// <param name="cache">The cache to look in.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="create">Creates the client when it is not cached yet.</param>
    /// <returns>The cached or newly created client.</returns>
    private static TClient GetOrCreate<TKey, TClient>(
        ConcurrentDictionary<TKey, Lazy<TClient>> cache,
        TKey key,
        Func<TKey, TClient> create)
        where TKey : notnull
    {
        var lazyClient = cache.GetOrAdd(
            key,
            static (cacheKey, createClient) => new Lazy<TClient>(
                () => createClient(cacheKey),
                LazyThreadSafetyMode.ExecutionAndPublication),
            create);

        try
        {
            return lazyClient.Value;
        }
        catch
        {
            cache.TryRemove(new KeyValuePair<TKey, Lazy<TClient>>(key, lazyClient));
            throw;
        }
    }

    private IKustoIngestClient CreateIngestClient(
        IngestClientCacheKey ingestClientCacheKey)
    {
        var options = monitor.Get(ingestClientCacheKey.ConnectionName);
        KustoIngestTargetValidator.ValidateConnection(options, ingestClientCacheKey.ConnectionName);

        return ingestClientFactory.Create(
            options.HostAddress!,
            options.Credential!,
            options.IngestUploadContainers,
            ingestClientCacheKey.Mode);
    }

    private ICslQueryProvider CreateQueryClient(ClientCacheKey clientCacheKey)
        => dataClientFactory.CreateQueryClient(
            GetConnectionString(clientCacheKey));

    private ICslAdminProvider CreateAdminClient(ClientCacheKey clientCacheKey)
        => dataClientFactory.CreateAdminClient(
            GetConnectionString(clientCacheKey));

    /// <summary>
    /// Builds the connection string for a query or admin client.
    /// </summary>
    /// <remarks>
    /// The database is the per-call <see cref="ClientCacheKey.DatabaseName"/> when given, otherwise
    /// <see cref="AtcKustoOptions.DatabaseName"/>. A <see cref="AtcKustoOptions.HostAddress"/> needs one of
    /// them; a <see cref="AtcKustoOptions.ConnectionString"/> keeps its own database (or the SDK default)
    /// when neither is set.
    /// </remarks>
    private KustoConnectionStringBuilder GetConnectionString(
        ClientCacheKey clientCacheKey)
    {
        var options = monitor.Get(clientCacheKey.ConnectionName);
        var databaseName = clientCacheKey.DatabaseName ?? options.DatabaseName;

        var builder = options switch
        {
            { HostAddress: { } host } when databaseName is not null
                => new KustoConnectionStringBuilder(host.AbsoluteUri, databaseName),
            { ConnectionString: { } cs }
                => new KustoConnectionStringBuilder(cs),
            { HostAddress: not null }
                => throw new InvalidOperationException(
                    $"No database configured for kusto connection: {clientCacheKey.ConnectionName}. " +
                    "Set AtcKustoOptions.DatabaseName or pass a database name."),
            _ => throw new InvalidOperationException(
                $"Missing configuration for kusto connection: {clientCacheKey.ConnectionName}"),
        };

        if (databaseName is not null)
        {
            builder.InitialCatalog = databaseName;
        }

        return options.Credential is { } credential
            ? builder.WithAadAzureTokenCredentialsAuthentication(credential)
            : builder;
    }

    /// <summary>
    /// Disposes every cached client. Safe to call more than once; only the first call has an effect.
    /// </summary>
    /// <remarks>
    /// Idempotence matters because the instance is registered once and forwarded to both
    /// <see cref="IKustoClientProvider"/> and <see cref="IKustoIngestClientProvider"/>; the DI container
    /// tracks the instance per registration and may dispose it more than once on shutdown.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        GC.SuppressFinalize(this);

        DisposeCreatedClients(adminClients);
        DisposeCreatedClients(queryClients);
        DisposeCreatedClients(ingestClients);
    }

    /// <summary>
    /// Disposes the clients in <paramref name="cache"/> that were created; a creation that failed or
    /// never ran has nothing to dispose.
    /// </summary>
    /// <typeparam name="TKey">The cache key type.</typeparam>
    /// <typeparam name="TClient">The client type.</typeparam>
    /// <param name="cache">The cache whose clients are disposed.</param>
    private static void DisposeCreatedClients<TKey, TClient>(
        ConcurrentDictionary<TKey, Lazy<TClient>> cache)
        where TKey : notnull
        where TClient : IDisposable
    {
        foreach (var lazyClient in cache.Values.Where(x => x.IsValueCreated))
        {
            lazyClient.Value.Dispose();
        }
    }
}