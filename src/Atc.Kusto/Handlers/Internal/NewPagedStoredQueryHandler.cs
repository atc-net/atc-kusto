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

    /// <summary>
    /// Creates the stored query result and returns its first page.
    /// </summary>
    /// <remarks>
    /// The stored result is created with a control command, which the Kusto SDK cannot cancel, so
    /// <paramref name="cancellationToken"/> is checked before the command is sent, and a command that
    /// has started always runs to completion.
    /// </remarks>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The first page, or <see langword="null"/> when the query returns no result.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled before the command is sent, or the
    /// command fails while it is cancelled.
    /// </exception>
    public async Task<PagedResult<T>?> Execute(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var queryId = queryIdProvider.Create(
            query.GetType(),
            sessionId);

        var header = $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = {pageSize}, expiresAfter = 1h) <|";
        const string footer = "| serialize row_number = row_number()";
        var queryText = $"{header}\n{query.GetQueryText().Trim(' ', '\n', '\t', ';')}\n{footer}";

        using var reader = await ExecuteControlCommand(queryText, cancellationToken);

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

    private async Task<IDataReader> ExecuteControlCommand(
        string queryText,
        CancellationToken cancellationToken)
    {
        try
        {
            return await adminProvider
                .ExecuteControlCommandAsync(
                    databaseName: null,
                    queryText,
                    query.GetClientRequestProperties());
        }
        catch (Exception ex) when (CancellationExceptionUtilities.IsCancellation(ex, cancellationToken))
        {
            throw CancellationExceptionUtilities.NormalizeCancellationException(ex, cancellationToken);
        }
    }
}