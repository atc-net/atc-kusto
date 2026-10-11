namespace Atc.Kusto.Factories.Internal;

/// <inheritdoc />
internal sealed class ScriptHandlerFactory : IScriptHandlerFactory
{
    private static readonly TimeSpan MinPagedResultExpiration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxPagedResultExpiration = TimeSpan.FromHours(24);

    private readonly ILoggerFactory loggerFactory;
    private readonly ResiliencePipeline resiliencePipeline;
    private readonly IQueryIdProvider queryIdProvider;
    private readonly IKustoClientProvider clientProvider;
    private readonly IOptionsMonitor<AtcKustoOptions> optionsMonitor;

    public ScriptHandlerFactory(
        ILoggerFactory loggerFactory,
        [FromKeyedServices(Constants.ResiliencePipelineKey)] ResiliencePipeline resiliencePipeline,
        IQueryIdProvider queryIdProvider,
        IKustoClientProvider clientProvider,
        IOptionsMonitor<AtcKustoOptions> optionsMonitor)
    {
        this.loggerFactory = loggerFactory;
        this.resiliencePipeline = resiliencePipeline;
        this.queryIdProvider = queryIdProvider;
        this.clientProvider = clientProvider;
        this.optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    public IScriptHandler Create(
        IKustoCommand command,
        string? connectionName = null,
        string? databaseName = null)
        => new SimpleCommandHandler(
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            command);

    /// <inheritdoc />
    public IScriptHandler<T> Create<T>(
        IKustoQuery<T> query,
        string? connectionName = null,
        string? databaseName = null,
        AtcQueryOptions? options = null)
    {
        options ??= new AtcQueryOptions();

        return new SimpleQueryHandler<T>(
            loggerFactory.CreateLogger<SimpleQueryHandler<T>>(),
            resiliencePipeline,
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            query,
            GetSerializerOptions(connectionName),
            options);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="AtcKustoOptions.PagedResultExpiration"/> of the connection is not between 1 second and 24 hours.
    /// </exception>
    public IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        string? continuationToken,
        string? connectionName = null,
        string? databaseName = null,
        AtcPagedQueryOptions? options = null)
    {
        var expiration = GetPagedResultExpiration(connectionName);

        return continuationToken is null
            ? new NewPagedStoredQueryHandler<T>(
                resiliencePipeline,
                queryIdProvider,
                clientProvider.GetAdminClient(
                    connectionName,
                    databaseName),
                query,
                GetSerializerOptions(connectionName),
                sessionId,
                pageSize,
                expiration,
                includeTotalCount: options?.IncludeTotalCount ?? false)
            : new ExistingPagedStoredQueryHandler<T>(
                loggerFactory.CreateLogger<ExistingPagedStoredQueryHandler<T>>(),
                resiliencePipeline,
                queryIdProvider,
                clientProvider.GetAdminClient(
                    connectionName,
                    databaseName),
                clientProvider.GetQueryClient(
                    connectionName,
                    databaseName),
                query,
                GetSerializerOptions(connectionName),
                sessionId,
                pageSize,
                expiration,
                continuationToken);
    }

    /// <inheritdoc />
    public IScriptHandler<StreamingQueryResult<T>?> CreateBuffered<T>(
        IKustoStreamingQuery<T> query,
        string? connectionName = null,
        string? databaseName = null,
        AtcStreamingQueryOptions? options = null)
    {
        options ??= new AtcStreamingQueryOptions();

        return new BufferedStreamingQueryHandler<T>(
            loggerFactory.CreateLogger<BufferedStreamingQueryHandler<T>>(),
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            query,
            GetSerializerOptions(connectionName),
            options);
    }

    /// <inheritdoc />
    public IStreamingScriptHandler<T?> Create<T>(
        IKustoStreamingQuery<T> query,
        string? connectionName = null,
        string? databaseName = null,
        AtcStreamingQueryOptions? options = null)
    {
        options ??= new AtcStreamingQueryOptions();

        return new StreamingQueryHandler<T?>(
            loggerFactory.CreateLogger<StreamingQueryHandler<T?>>(),
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            query,
            GetSerializerOptions(connectionName),
            options);
    }

    /// <summary>
    /// Gets <see cref="AtcKustoOptions.PagedResultExpiration"/> of the connection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when it is not between 1 second and 24 hours.</exception>
    private TimeSpan GetPagedResultExpiration(string? connectionName)
    {
        var expiration = optionsMonitor
            .Get(connectionName)
            .PagedResultExpiration;

        if (expiration < MinPagedResultExpiration ||
            expiration > MaxPagedResultExpiration)
        {
            throw new InvalidOperationException(
                $"{nameof(AtcKustoOptions.PagedResultExpiration)} must be between 1 second and 24 hours, but was {expiration}.");
        }

        return expiration;
    }

    /// <summary>
    /// Gets <see cref="AtcKustoOptions.SerializerOptions"/> of the connection.
    /// </summary>
    private JsonSerializerOptions GetSerializerOptions(string? connectionName)
        => optionsMonitor
            .Get(connectionName)
            .SerializerOptions;
}