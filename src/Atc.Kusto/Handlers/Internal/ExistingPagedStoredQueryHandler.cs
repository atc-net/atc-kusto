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
        string continuationToken)
        : this(logger, resiliencePipeline, queryIdProvider, adminProvider: null!, queryProvider, query, sessionId, pageSize, continuationToken)
        => adminProvider = null;

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
        if (PagedContinuationToken.Parse(continuationToken) is not { } token)
        {
            return null;
        }

        // The queryId is calculated on the server, never read from the token.
        var queryId = queryIdProvider.Create(
            query,
            sessionId,
            token.Nonce);

        var itemsReturned = token.ItemsReturned;
        var firstRowNum = itemsReturned + 1;
        var lastRowNum = itemsReturned + pageSize;

        var queryText = $"stored_query_result('{queryId}') | where row_number between({firstRowNum} .. {lastRowNum})";

        try
        {
            return await resiliencePipeline.ExecuteAsync(
                async context =>
                {
                    var clientRequestProperties = query.GetClientRequestProperties();

                    // No specific AtcQueryOptions available here; default to enabling server-side cancel when adminProvider is present.
                    using var serverSideCancellationRegistration = adminProvider?.RegisterKustoServerSideCancellation(
                        logger,
                        databaseName: null,
                        clientRequestProperties,
                        context);

                    using var reader = await queryProvider
                        .ExecuteQueryAsync(
                            databaseName: null,
                            queryText,
                            clientRequestProperties,
                            context);

                    var items = query.ReadResult(reader);
                    if (items is null)
                    {
                        return null;
                    }

                    var newContinuationToken = items.Count < pageSize
                        ? null
                        : new PagedContinuationToken(token.Nonce, itemsReturned + items.Count).ToString();

                    return new PagedResult<T>(items, newContinuationToken);
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
}