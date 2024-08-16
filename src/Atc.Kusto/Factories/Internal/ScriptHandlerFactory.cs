namespace Atc.Kusto.Factories.Internal;

/// <inheritdoc />
internal sealed class ScriptHandlerFactory : IScriptHandlerFactory
{
    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly ICslAdminProvider adminProvider;

    public ScriptHandlerFactory(
        IQueryIdProvider queryIdProvider,
        ICslQueryProvider queryProvider,
        ICslAdminProvider adminProvider)
    {
        this.queryIdProvider = queryIdProvider;
        this.queryProvider = queryProvider;
        this.adminProvider = adminProvider;
    }

    /// <inheritdoc />
    public IScriptHandler Create(
        IKustoCommand command)
        => new SimpleCommandHandler(
            adminProvider,
            command);

    /// <inheritdoc />
    public IScriptHandler<T> Create<T>(
        IKustoQuery<T> query)
        => new SimpleQueryHandler<T>(
            queryProvider,
            query);

    /// <inheritdoc />
    public IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        string? continuationToken)
        => continuationToken is null
            ? new NewPagedStoredQueryHandler<T>(
                queryIdProvider,
                adminProvider,
                query,
                sessionId,
                pageSize)
            : new ExistingPagedStoredQueryHandler<T>(
                queryProvider,
                query,
                pageSize,
                continuationToken);
}