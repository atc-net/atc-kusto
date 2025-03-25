namespace Atc.Kusto.Factories.Internal;

/// <inheritdoc />
internal sealed class ScriptHandlerFactory : IScriptHandlerFactory
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ResiliencePipeline resiliencePipeline;
    private readonly IQueryIdProvider queryIdProvider;
    private readonly IKustoClientProvider clientProvider;

    public ScriptHandlerFactory(
        ILoggerFactory loggerFactory,
        [FromKeyedServices(Constants.ResiliencePipelineKey)] ResiliencePipeline resiliencePipeline,
        IQueryIdProvider queryIdProvider,
        IKustoClientProvider clientProvider)
    {
        this.loggerFactory = loggerFactory;
        this.resiliencePipeline = resiliencePipeline;
        this.queryIdProvider = queryIdProvider;
        this.clientProvider = clientProvider;
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
        string? databaseName = null)
        => new SimpleQueryHandler<T>(
            loggerFactory.CreateLogger<SimpleQueryHandler<T>>(),
            resiliencePipeline,
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            query);

    /// <inheritdoc />
    public IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        string? continuationToken,
        string? connectionName = null,
        string? databaseName = null)
        => continuationToken is null
            ? new NewPagedStoredQueryHandler<T>(
                queryIdProvider,
                clientProvider.GetAdminClient(
                    connectionName,
                    databaseName),
                query,
                sessionId,
                pageSize)
            : new ExistingPagedStoredQueryHandler<T>(
                loggerFactory.CreateLogger<ExistingPagedStoredQueryHandler<T>>(),
                resiliencePipeline,
                clientProvider.GetQueryClient(
                    connectionName,
                    databaseName),
                query,
                pageSize,
                continuationToken);
}