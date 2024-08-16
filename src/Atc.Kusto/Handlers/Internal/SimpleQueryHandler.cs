namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// A simple query handler that executes a Kusto query and returns a result of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the result returned by the query.</typeparam>
internal sealed class SimpleQueryHandler<T> : IScriptHandler<T>
{
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<T> query;

    public SimpleQueryHandler(
        ICslQueryProvider queryProvider,
        IKustoQuery<T> query)
    {
        this.queryProvider = queryProvider;
        this.query = query;
    }

    /// <summary>
    /// Executes the Kusto query asynchronously and returns the result of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the query result of type <typeparamref name="T"/> or null if the result is not available.
    /// </returns>
    public async Task<T?> Execute(
        CancellationToken cancellationToken)
    {
        using var reader = await queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties(),
                cancellationToken);

        return query.ReadResult(reader);
    }
}