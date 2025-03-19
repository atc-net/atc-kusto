namespace Atc.Kusto.Factories.Internal;

/// <inheritdoc />
internal sealed class ScriptHandlerFactory : IScriptHandlerFactory
{
    private readonly IQueryIdProvider queryIdProvider;
    private readonly IKustoClientProvider clientProvider;

    public ScriptHandlerFactory(
        IQueryIdProvider queryIdProvider,
        IKustoClientProvider clientProvider)
    {
        this.queryIdProvider = queryIdProvider;
        this.clientProvider = clientProvider;
    }

    /// <inheritdoc />
    public IScriptHandler Create(
        IKustoCommand command)
        => new SimpleCommandHandler(
            clientProvider.GetAdminClient(),
            command);

    /// <inheritdoc />
    public IScriptHandler<T> Create<T>(
        IKustoQuery<T> query)
        => new SimpleQueryHandler<T>(
            clientProvider.GetQueryClient(),
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
                clientProvider.GetAdminClient(),
                query,
                sessionId,
                pageSize)
            : new ExistingPagedStoredQueryHandler<T>(
                clientProvider.GetQueryClient(),
                query,
                pageSize,
                continuationToken);
}