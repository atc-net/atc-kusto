namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// <![CDATA[
/// Handles the execution of a stored Kusto query that retrieves paginated results.
/// Implements the <see cref="IScriptHandler{PagedResult{T}}"/> interface.
/// ]]>
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
internal sealed class ExistingPagedStoredQueryHandler<T> : IScriptHandler<PagedResult<T>>
{
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<IReadOnlyList<T>> query;
    private readonly int pageSize;
    private readonly string continuationToken;

    public ExistingPagedStoredQueryHandler(
        ICslQueryProvider queryProvider,
        IKustoQuery<IReadOnlyList<T>> query,
        int pageSize,
        string continuationToken)
    {
        this.queryProvider = queryProvider;
        this.query = query;
        this.pageSize = pageSize;
        this.continuationToken = continuationToken;
    }

    /// <summary>
    /// Executes the stored Kusto query asynchronously and returns a paginated result set.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="PagedResult{T}"/>
    /// if the query succeeds, or null if the continuation token is invalid or the query fails.
    /// </returns>
    [SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "OK - Not needed for long")]
    public async Task<PagedResult<T>?> Execute(
        CancellationToken cancellationToken)
    {
        var split = continuationToken.Split(';');
        if (split.Length != 2)
        {
            return null;
        }

        var queryId = split[0].ToAlphanumeric();

        if (!long.TryParse(split[1], GlobalizationConstants.EnglishCultureInfo, out var itemsReturned))
        {
            return null;
        }

        var firstRowNum = itemsReturned + 1;
        var lastRowNum = itemsReturned + pageSize;

        var queryText = $"stored_query_result('{queryId}') | where row_number between({firstRowNum} .. {lastRowNum})";

        try
        {
            using var reader = await queryProvider
                .ExecuteQueryAsync(
                    databaseName: null,
                    queryText,
                    query.GetClientRequestProperties(),
                    cancellationToken);

            var items = query.ReadResult(reader);
            if (items is null)
            {
                return null;
            }

            var newContinuationToken = items.Count < pageSize
                ? null
                : $"{queryId};{itemsReturned + items.Count}";

            return new PagedResult<T>(items, newContinuationToken);
        }
        catch (SemanticException)
        {
            // TODO: Log error
            return null;
        }
    }
}