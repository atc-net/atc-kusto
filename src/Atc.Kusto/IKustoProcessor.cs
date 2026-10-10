namespace Atc.Kusto;

/// <summary>
/// Provides functionality to process and execute Kusto commands and queries.
/// </summary>
/// <remarks>
/// When the <c>cancellationToken</c> is cancelled, every method ends with an
/// <see cref="OperationCanceledException"/>, never with <see langword="null"/>: the Kusto SDK's own
/// cancellation errors are translated, with the original as <see cref="Exception.InnerException"/>.
/// Those errors are only translated when the token was cancelled; one raised for another reason (e.g. a
/// <c>.cancel query</c> run by someone else) is handled as an ordinary failure.
/// </remarks>
public interface IKustoProcessor
{
    /// <summary>
    /// Executes a Kusto command asynchronously using a script handler created by the factory.
    /// </summary>
    /// <remarks>
    /// The Kusto SDK cannot cancel a control command, so <paramref name="cancellationToken"/> is checked
    /// before the command is sent; a command that has started runs to completion. The outcome you see
    /// therefore always matches what happened on the cluster.
    /// </remarks>
    /// <param name="command">The Kusto command to be executed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled before the command is sent, or the
    /// command fails while it is cancelled.
    /// </exception>
    Task ExecuteCommand(
        IKustoCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a Kusto query asynchronously using a script handler created by the factory, and returns the result of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the query.</typeparam>
    /// <param name="query">The Kusto query to be executed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.
    /// The task result contains the query result of type <typeparamref name="T"/> or null if no result is available.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<T?> ExecuteQuery<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a Kusto query asynchronously using a script handler created by the factory, and returns the result of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the query.</typeparam>
    /// <param name="query">The Kusto query to be executed.</param>
    /// <param name="options">Optional streaming options. If null, default options are used.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.
    /// The task result contains the query result of type <typeparamref name="T"/> or null if no result is available.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<T?> ExecuteQuery<T>(
        IKustoQuery<T> query,
        AtcQueryOptions? options = null,
        CancellationToken cancellationToken = default);

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
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<PagedResult<T>?> ExecutePagedQuery<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int? pageSize,
        string? continuationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a streaming query and returns a result containing both the rows and optional frame data
    /// (e.g. header, table schema, and completion summary).
    /// </summary>
    /// <typeparam name="T">The type to which each row is mapped.</typeparam>
    /// <param name="query">The Kusto streaming query to execute.</param>
    /// <param name="options">Optional streaming options. If null, default options are used.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="StreamingQueryResult{T}"/>
    /// with the rows and additional frame data, or null if no result is available.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<StreamingQueryResult<T>?> ExecuteBufferedStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        AtcStreamingQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a streaming query using the default streaming options and returns a result containing both
    /// the rows and optional frame data (e.g. header, table schema, and completion summary).
    /// </summary>
    /// <typeparam name="T">The type to which each row is mapped.</typeparam>
    /// <param name="query">The Kusto streaming query to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="StreamingQueryResult{T}"/>
    /// with the rows and additional frame data, or null if no result is available.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<StreamingQueryResult<T>?> ExecuteBufferedStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a streaming query and returns an asynchronous stream of rows.
    /// This overload returns only the rows, without any additional frame data.
    /// </summary>
    /// <typeparam name="T">The type to which each row is mapped.</typeparam>
    /// <param name="query">The Kusto streaming query to execute.</param>
    /// <param name="options">Optional streaming options. If null, default options are used.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An asynchronous stream of rows mapped to type <typeparamref name="T"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    IAsyncEnumerable<T> ExecuteStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        AtcStreamingQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a streaming query and returns an asynchronous stream of rows.
    /// This overload returns only the rows, without any additional frame data.
    /// </summary>
    /// <typeparam name="T">The type to which each row is mapped.</typeparam>
    /// <param name="query">The Kusto streaming query to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An asynchronous stream of rows mapped to type <typeparamref name="T"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    IAsyncEnumerable<T> ExecuteStreamingQuery<T>(
        IKustoStreamingQuery<T> query,
        CancellationToken cancellationToken = default);
}