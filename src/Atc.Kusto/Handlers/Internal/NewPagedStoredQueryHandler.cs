namespace Atc.Kusto.Handlers.Internal;

internal sealed class NewPagedStoredQueryHandler<T> : IScriptHandler<PagedResult<T>>
{
    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslAdminProvider adminProvider;
    private readonly IKustoQuery<IReadOnlyList<T>> query;
    private readonly string? sessionId;
    private readonly int pageSize;

    public NewPagedStoredQueryHandler(
        IQueryIdProvider queryIdProvider,
        ICslAdminProvider adminProvider,
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize)
    {
        this.queryIdProvider = queryIdProvider;
        this.adminProvider = adminProvider;
        this.query = query;
        this.sessionId = sessionId;
        this.pageSize = pageSize;
    }

    public async Task<PagedResult<T>?> Execute(
        CancellationToken cancellationToken)
    {
        var queryId = queryIdProvider.Create(
            query.GetType(),
            sessionId);

        var header = $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = {pageSize}, expiresAfter = 1h) <|";
        const string footer = "| serialize row_number = row_number()";
        var queryText = $"{header}\n{query.GetQueryText().Trim(' ', '\n', '\t', ';')}\n{footer}";

        using var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                queryText,
                query.GetClientRequestProperties());

        var items = query.ReadResult(reader);
        if (items is null)
        {
            return null;
        }

        var continuationToken = items.Count < pageSize
            ? null
            : $"{queryId};{items.Count}";

        return new PagedResult<T>(items, continuationToken);
    }
}