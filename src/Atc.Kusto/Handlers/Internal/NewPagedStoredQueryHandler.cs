namespace Atc.Kusto.Handlers.Internal;

internal sealed class NewPagedStoredQueryHandler<T> : IScriptHandler<PagedResult<T>>
{
    private readonly ResiliencePipeline resiliencePipeline;
    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslAdminProvider adminProvider;
    private readonly IKustoQuery<IReadOnlyList<T>> query;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly string? sessionId;
    private readonly int pageSize;
    private readonly TimeSpan expiration;
    private readonly bool includeTotalCount;

    public NewPagedStoredQueryHandler(
        [FromKeyedServices(Constants.ResiliencePipelineKey)] ResiliencePipeline resiliencePipeline,
        IQueryIdProvider queryIdProvider,
        ICslAdminProvider adminProvider,
        IKustoQuery<IReadOnlyList<T>> query,
        JsonSerializerOptions serializerOptions,
        string? sessionId,
        int pageSize,
        TimeSpan expiration,
        bool includeTotalCount)
    {
        this.resiliencePipeline = resiliencePipeline;
        this.queryIdProvider = queryIdProvider;
        this.adminProvider = adminProvider;
        this.query = query;
        this.serializerOptions = serializerOptions;
        this.sessionId = sessionId;
        this.pageSize = pageSize;
        this.expiration = expiration;
        this.includeTotalCount = includeTotalCount;
    }

    /// <summary>
    /// Creates the stored query result and returns its first page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The stored result is created with a control command, which the Kusto SDK cannot cancel, so
    /// <paramref name="cancellationToken"/> is checked before the command is sent, and a command that
    /// has started always runs to completion.
    /// </para>
    /// <para>
    /// Temporary errors (e.g. throttling) are retried by the resilience pipeline; creating the stored result again
    /// is safe, as the command replaces it. Other errors are thrown to the caller.
    /// </para>
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

        // Without a session, a random nonce gives each paging run its own stored result.
        // The continuation token carries the nonce, never the queryId.
        var nonce = sessionId is null
            ? Guid.NewGuid().ToString("N")
            : null;

        var queryId = queryIdProvider.Create(
            query,
            sessionId,
            nonce);

        var queryText = StoredQueryResultCommands.CreateSetOrReplace(
            query,
            queryId,
            pageSize,
            expiration);

        var items = await ExecuteControlCommand(queryText, cancellationToken);
        if (items is null)
        {
            return null;
        }

        if (items.Count < pageSize)
        {
            // Not a full page, so these items are all there is.
            return new PagedResult<T>(items, ContinuationToken: null)
            {
                TotalCount = includeTotalCount ? items.Count : null,
            };
        }

        var totalCount = includeTotalCount
            ? await ReadTotalCount(queryId, cancellationToken)
            : null;

        var continuationToken = PagedContinuationToken.CreateNext(
            nonce,
            itemsReturned: items.Count,
            pageItemCount: items.Count,
            pageSize,
            totalCount);

        return new PagedResult<T>(items, continuationToken?.ToString())
        {
            TotalCount = totalCount,
        };
    }

    /// <summary>
    /// Reads the number of rows in the stored result from its metadata, without running the query again.
    /// </summary>
    private async Task<long?> ReadTotalCount(
        string queryId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await resiliencePipeline.ExecuteAsync(
                async context =>
                {
                    // The command can't be cancelled, so it isn't sent once the caller has cancelled.
                    context.ThrowIfCancellationRequested();

                    using var reader = await adminProvider
                        .ExecuteControlCommandAsync(
                            databaseName: null,
                            StoredQueryResultCommands.CreateShowStoredQueryResult(queryId),
                            query.GetClientRequestProperties());

                    return StoredQueryResultCommands.ReadRowCount(reader);
                },
                cancellationToken);
        }
        catch (Exception ex) when (CancellationExceptionUtilities.IsCancellation(ex, cancellationToken))
        {
            throw CancellationExceptionUtilities.NormalizeCancellationException(ex, cancellationToken);
        }
    }

    /// <summary>
    /// Runs the command that creates the stored result, and reads the first page it returns.
    /// </summary>
    private async Task<IReadOnlyList<T>?> ExecuteControlCommand(
        string queryText,
        CancellationToken cancellationToken)
    {
        try
        {
            return await resiliencePipeline.ExecuteAsync(
                async context =>
                {
                    // Also checked before a retry: the command itself can't be cancelled.
                    context.ThrowIfCancellationRequested();

                    using var reader = await adminProvider
                        .ExecuteControlCommandAsync(
                            databaseName: null,
                            queryText,
                            query.GetClientRequestProperties());

                    return query.ReadResult(reader, serializerOptions);
                },
                cancellationToken);
        }
        catch (Exception ex) when (CancellationExceptionUtilities.IsCancellation(ex, cancellationToken))
        {
            throw CancellationExceptionUtilities.NormalizeCancellationException(ex, cancellationToken);
        }
    }
}