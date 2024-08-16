namespace Atc.Kusto;

/// <summary>
/// Provides functionality to process and execute Kusto commands and queries.
/// </summary>
public interface IKustoProcessor
{
    /// <summary>
    /// Executes a Kusto command asynchronously using a script handler created by the factory.
    /// </summary>
    /// <param name="command">The Kusto command to be executed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteCommand(
        IKustoCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a Kusto query asynchronously using a script handler created by the factory, and returns the result of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the query.</typeparam>
    /// <param name="query">The Kusto query to be executed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.
    /// The task result contains the query result of type <typeparamref name="T"/> or null if no result is available.</returns>
    Task<T?> ExecuteQuery<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a Kusto query asynchronously using a script handler created by the factory, and returns a paginated result set.
    /// </summary>
    /// <typeparam name="T">The type of items in the result set.</typeparam>
    /// <param name="query">The Kusto query to be executed.</param>
    /// <param name="sessionId">An optional session ID for tracking the query execution.</param>
    /// <param name="pageSize">The number of items per page in the result set.</param>
    /// <param name="continuationToken">An optional token to continue fetching results from a previous query execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.The task result contains the paginated result set.</returns>
    Task<PagedResult<T>?> ExecutePagedQuery<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        string? continuationToken,
        CancellationToken cancellationToken);
}