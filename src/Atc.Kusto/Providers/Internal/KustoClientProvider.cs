namespace Atc.Kusto.Providers.Internal;

public sealed class KustoClientProvider : IDisposable, IKustoClientProvider, IKustoIngestClientProvider
{
    private readonly ConcurrentDictionary<ClientCacheKey, ICslQueryProvider> queryClients = new();
    private readonly ConcurrentDictionary<ClientCacheKey, ICslAdminProvider> adminClients = new();
    private readonly ConcurrentDictionary<IngestClientCacheKey, Lazy<IKustoIngestClient>> ingestClients = new();

    private readonly IOptionsMonitor<AtcKustoOptions> monitor;
    private readonly IKustoIngestClientFactory ingestClientFactory;

    private int disposed;

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
        KustoIngestTargetValidator.ValidateConnection(options, ingestClientCacheKey.ConnectionName);

        return ingestClientFactory.Create(
            options.HostAddress!,
            options.Credential!,
            options.IngestUploadContainers,
            ingestClientCacheKey.Mode);
    }

    private ICslQueryProvider CreateQueryClient(ClientCacheKey clientCacheKey)
        => KustoClientFactory.CreateCslQueryProvider(
            GetConnectionString(clientCacheKey));

    private ICslAdminProvider CreateAdminClient(ClientCacheKey clientCacheKey)
        => KustoClientFactory.CreateCslAdminProvider(
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