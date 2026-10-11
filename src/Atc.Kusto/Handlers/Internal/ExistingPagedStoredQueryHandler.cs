namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// <![CDATA[
/// Handles the execution of a stored Kusto query that retrieves paginated results.
/// Implements the <see cref="IScriptHandler{PagedResult{T}}"/> interface.
/// ]]>
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
internal sealed partial class ExistingPagedStoredQueryHandler<T> : IScriptHandler<PagedResult<T>>
{
    private readonly ResiliencePipeline resiliencePipeline;
    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslAdminProvider? adminProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<IReadOnlyList<T>> query;
    private readonly string? sessionId;
    private readonly int pageSize;
    private readonly TimeSpan expiration;
    private readonly string continuationToken;

    public ExistingPagedStoredQueryHandler(
        ILogger<ExistingPagedStoredQueryHandler<T>> logger,
        [FromKeyedServices(Constants.ResiliencePipelineKey)] ResiliencePipeline resiliencePipeline,
        IQueryIdProvider queryIdProvider,
        ICslAdminProvider adminProvider,
        ICslQueryProvider queryProvider,
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        TimeSpan expiration,
        string continuationToken)
    {
        this.logger = logger;
        this.resiliencePipeline = resiliencePipeline;
        this.queryIdProvider = queryIdProvider;
        this.adminProvider = adminProvider;
        this.queryProvider = queryProvider;
        this.query = query;
        this.sessionId = sessionId;
        this.pageSize = pageSize;
        this.expiration = expiration;
        this.continuationToken = continuationToken;
    }

    // Backward-compatible overload (pre-cancellation change)
    public ExistingPagedStoredQueryHandler(
        ILogger<ExistingPagedStoredQueryHandler<T>> logger,
        [FromKeyedServices(Constants.ResiliencePipelineKey)] ResiliencePipeline resiliencePipeline,
        IQueryIdProvider queryIdProvider,
        ICslQueryProvider queryProvider,
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        TimeSpan expiration,
        string continuationToken)
        : this(logger, resiliencePipeline, queryIdProvider, adminProvider: null!, queryProvider, query, sessionId, pageSize, expiration, continuationToken)
        => adminProvider = null;

    /// <summary>
    /// Executes the stored Kusto query asynchronously and returns a paginated result set.
    /// </summary>
    /// <remarks>
    /// When the stored query result no longer exists (it has expired), the query runs again into the
    /// same queryId and the requested page is read from the new result. The data may have changed in between,
    /// so rows can shift between pages; a query without <c>order by</c> can even return its rows in another order.
    /// </remarks>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="PagedResult{T}"/>
    /// if the query succeeds, or null if the continuation token is invalid or the query fails.
    /// </returns>
    public async Task<PagedResult<T>?> Execute(
        CancellationToken cancellationToken)
    {
        if (PagedContinuationToken.Parse(continuationToken) is not { } token)
        {
            return null;
        }

        // The queryId is calculated on the server, never read from the token.
        var queryId = queryIdProvider.Create(
            query,
            sessionId,
            token.Nonce);

        try
        {
            return await resiliencePipeline.ExecuteAsync(
                async context =>
                {
                    try
                    {
                        return await ReadPage(
                            queryId,
                            token,
                            token.TotalCount,
                            context);
                    }
                    catch (SemanticException) when (adminProvider is not null)
                    {
                        // Kusto reports a stored query result that doesn't exist (anymore) as a semantic error.
                        LogStoredQueryResultNotFound(queryId);

                        await RecreateStoredQueryResult(
                            adminProvider,
                            queryId,
                            context);

                        // The new run can have another number of rows.
                        var totalCount = token.TotalCount is null
                            ? null
                            : await ReadTotalCount(
                                adminProvider,
                                queryId,
                                context);

                        return await ReadPage(
                            queryId,
                            token,
                            totalCount,
                            context);
                    }
                },
                cancellationToken);
        }
        catch (Exception ex) when (CancellationExceptionUtilities.IsCancellation(ex, cancellationToken))
        {
            throw CancellationExceptionUtilities.NormalizeCancellationException(ex, cancellationToken);
        }
        catch (KustoServicePartialQueryFailureException ex)
        {
            LogKustoServicePartialQueryFailureException(
                ex,
                ex.ClientRequestId,
                ex.Query);

            return null;
        }
        catch (KustoServiceException ex)
        {
            LogKustoServiceException(
                ex,
                ex.ClientRequestId);

            return null;
        }
        catch (SemanticException ex)
        {
            LogSemanticException(
                ex,
                ex.ClientRequestId,
                ex.Text,
                ex.SemanticErrors);

            return null;
        }
        catch (Exception ex)
        {
            LogUnhandledException(ex);
            return null;
        }
    }

    /// <summary>
    /// Reads the page after <see cref="PagedContinuationToken.ItemsReturned"/> from the stored query result.
    /// </summary>
    /// <exception cref="SemanticException">Thrown when the stored query result doesn't exist.</exception>
    private async Task<PagedResult<T>?> ReadPage(
        string queryId,
        PagedContinuationToken token,
        long? totalCount,
        CancellationToken cancellationToken)
    {
        var clientRequestProperties = query.GetClientRequestProperties();

        // No specific AtcQueryOptions available here; default to enabling server-side cancel when adminProvider is present.
        using var serverSideCancellationRegistration = adminProvider?.RegisterKustoServerSideCancellation(
            logger,
            databaseName: null,
            clientRequestProperties,
            cancellationToken);

        using var reader = await queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                StoredQueryResultCommands.CreatePageQuery(
                    queryId,
                    token.ItemsReturned,
                    pageSize),
                clientRequestProperties,
                cancellationToken);

        var items = query.ReadResult(reader);
        if (items is null)
        {
            return null;
        }

        // A page past the last row is empty, which is a valid page and ends the paging.
        var newContinuationToken = PagedContinuationToken.CreateNext(
            token.Nonce,
            itemsReturned: token.ItemsReturned + items.Count,
            pageItemCount: items.Count,
            pageSize,
            totalCount);

        return new PagedResult<T>(items, newContinuationToken?.ToString())
        {
            TotalCount = totalCount,
        };
    }

    /// <summary>
    /// Runs the query again and stores its result under <paramref name="queryId"/>.
    /// </summary>
    /// <remarks>
    /// The control command can't be cancelled by the Kusto SDK, so <paramref name="cancellationToken"/> is checked
    /// before it is sent.
    /// </remarks>
    private async Task RecreateStoredQueryResult(
        ICslAdminProvider admin,
        string queryId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var reader = await admin.ExecuteControlCommandAsync(
            databaseName: null,
            StoredQueryResultCommands.CreateSetOrReplace(
                query,
                queryId,
                pageSize,
                expiration),
            query.GetClientRequestProperties());
    }

    /// <summary>
    /// Reads the number of rows in the stored result from its metadata, without running the query again.
    /// </summary>
    private async Task<long?> ReadTotalCount(
        ICslAdminProvider admin,
        string queryId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var reader = await admin.ExecuteControlCommandAsync(
            databaseName: null,
            StoredQueryResultCommands.CreateShowStoredQueryResult(queryId),
            query.GetClientRequestProperties());

        return StoredQueryResultCommands.ReadRowCount(reader);
    }
}