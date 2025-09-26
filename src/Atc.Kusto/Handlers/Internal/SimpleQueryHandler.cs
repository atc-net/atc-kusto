namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// A simple query handler that executes a Kusto query and returns a result of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the result returned by the query.</typeparam>
internal sealed partial class SimpleQueryHandler<T> : IScriptHandler<T>
{
    private readonly ResiliencePipeline resiliencePipeline;
    private readonly ICslAdminProvider? adminProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<T> query;
    private readonly AtcQueryOptions queryOptions;

    public SimpleQueryHandler(
        ILogger<SimpleQueryHandler<T>> logger,
        [FromKeyedServices(Constants.ResiliencePipelineKey)] ResiliencePipeline resiliencePipeline,
        ICslAdminProvider adminProvider,
        ICslQueryProvider queryProvider,
        IKustoQuery<T> query,
        AtcQueryOptions queryOptions)
    {
        this.logger = logger;
        this.resiliencePipeline = resiliencePipeline;
        this.adminProvider = adminProvider;
        this.queryProvider = queryProvider;
        this.query = query;
        this.queryOptions = queryOptions;
    }

    // Backward-compatible overload (pre-cancellation change)
    public SimpleQueryHandler(
        ILogger<SimpleQueryHandler<T>> logger,
        [FromKeyedServices(Constants.ResiliencePipelineKey)] ResiliencePipeline resiliencePipeline,
        ICslQueryProvider queryProvider,
        IKustoQuery<T> query,
        AtcQueryOptions queryOptions)
        : this(logger, resiliencePipeline, adminProvider: null!, queryProvider, query, queryOptions)
    {
        if (queryOptions.EnableServerSideCancellation)
        {
            throw new ArgumentException(
                "Server-side cancellation cannot be enabled when adminProvider is not supplied.",
                nameof(queryOptions));
        }

        adminProvider = null;
    }

    /// <summary>
    /// Executes the Kusto query asynchronously and returns the result of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the query result of type <typeparamref name="T"/> or null if the result is not available.
    /// </returns>
    public async Task<T?> Execute(CancellationToken cancellationToken)
    {
        try
        {
            var clientRequestProperties = query.GetClientRequestProperties();

            clientRequestProperties.SetQueryOptions(queryOptions);

            using var serverSideCancellationRegistration = CancellationTokenKustoExtensions.ShouldEnableServerSideCancellation(adminProvider, queryOptions.EnableServerSideCancellation)
                ? adminProvider!.RegisterKustoServerSideCancellation(
                    logger,
                    databaseName: null,
                    clientRequestProperties,
                    cancellationToken)
                : null;

            return await resiliencePipeline.ExecuteAsync(
                async context =>
                {
                    using var reader = await queryProvider
                        .ExecuteQueryAsync(
                            databaseName: null,
                            query.GetQueryText(),
                            clientRequestProperties,
                            context);

                    return query.ReadResult(reader);
                },
                cancellationToken);
        }
        catch (Exception ex) when (CancellationTokenKustoExtensions.IsCancellationException(ex))
        {
            throw CancellationTokenKustoExtensions.NormalizeCancellationException(ex, cancellationToken);
        }
        catch (KustoServicePartialQueryFailureException ex)
        {
            LogKustoServicePartialQueryFailureException(
                ex,
                ex.ClientRequestId,
                ex.Query);

            return default;
        }
        catch (KustoServiceException ex)
        {
            LogKustoServiceException(
                ex,
                ex.ClientRequestId);

            return default;
        }
        catch (SemanticException ex)
        {
            LogSemanticException(
                ex,
                ex.ClientRequestId,
                ex.Text,
                ex.SemanticErrors);

            return default;
        }
        catch (Exception ex)
        {
            LogUnhandledException(ex);
            return default;
        }
    }
}