namespace Atc.Kusto.Factories;

/// <summary>
/// Factory interface for creating script handlers that can execute Kusto commands,
/// queries (single-result, paged, or streaming), and return strongly-typed results.
/// </summary>
public interface IScriptHandlerFactory
{
    /// <summary>
    /// Creates an <see cref="IScriptHandler"/> that executes a Kusto command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="connectionName">
    /// Optional name of the Kusto connection/cluster; the default connection is used when <see langword="null"/>.
    /// </param>
    /// <param name="databaseName">
    /// Optional database name; the default database is used when <see langword="null"/>.
    /// </param>
    /// <returns>An instance of <see cref="IScriptHandler"/> capable of executing the command.</returns>
    IScriptHandler Create(
        IKustoCommand command,
        string? connectionName = null,
        string? databaseName = null);

    /// <summary>
    /// Creates an <see cref="IScriptHandler{T}"/> that executes a Kusto query and returns a single result of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the query.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="connectionName">
    /// Optional name of the Kusto connection/cluster; the default connection is used when <see langword="null"/>.
    /// </param>
    /// <param name="databaseName">
    /// Optional database name; the default database is used when <see langword="null"/>.
    /// </param>
    /// <param name="options">
    /// Optional query-execution options; default options are applied when <see langword="null"/>.
    /// </param>
    /// <returns>An instance of <see cref="IScriptHandler{T}"/> capable of executing the query and returning the result.</returns>
    IScriptHandler<T> Create<T>(
        IKustoQuery<T> query,
        string? connectionName = null,
        string? databaseName = null,
        AtcQueryOptions? options = null);

    /// <summary>
    /// Creates an <see cref="IScriptHandler{T}"/> that executes a Kusto query and returns a paged result set.
    /// </summary>
    /// <typeparam name="T">The type of items in the result set.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="sessionId">An optional session ID for tracking the query execution.</param>1
    /// <param name="pageSize">The number of items per page in the result set.</param>
    /// <param name="continuationToken">An optional token to continue fetching results from a previous query execution.</param>
    /// <param name="connectionName">
    /// Optional name of the Kusto connection/cluster; the default connection is used when <see langword="null"/>.
    /// </param>
    /// <param name="databaseName">
    /// Optional database name; the default database is used when <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IScriptHandler{T}"/> capable of executing the paged query and returning a paged result
    /// of type <see cref="PagedResult{T}"/>.
    /// </returns>
    IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        string? continuationToken,
        string? connectionName = null,
        string? databaseName = null);

    /// <summary>
    /// Creates an <see cref="IScriptHandler{T}"/> that executes a streaming Kusto query
    /// and buffers the stream into a <see cref="StreamingQueryResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the streaming query result.</typeparam>
    /// <param name="query">The streaming query to execute.</param>
    /// <param name="connectionName">
    /// Optional name of the Kusto connection/cluster; the default connection is used when <see langword="null"/>.
    /// </param>
    /// <param name="databaseName">
    /// Optional database name; the default database is used when <see langword="null"/>.
    /// </param>
    /// <param name="options">
    /// Optional streaming-query options (timeouts, buffer size, etc.); default options are applied when <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IScriptHandler{T}"/> capable of executing the streaming query and returning a streaming query result
    /// of type <see cref="StreamingQueryResult{T}"/>.
    /// </returns>
    IScriptHandler<StreamingQueryResult<T>?> CreateBuffered<T>(
        IKustoStreamingQuery<T> query,
        string? connectionName = null,
        string? databaseName = null,
        AtcStreamingQueryOptions? options = null);

    /// <summary>
    /// Creates an <see cref="IStreamingScriptHandler{T}"/> that executes a streaming Kusto query
    /// and yields the results as they arrive.
    /// </summary>
    /// <typeparam name="T">The type of items in the stream.</typeparam>
    /// <param name="query">The streaming query to execute.</param>
    /// <param name="connectionName">
    /// Optional name of the Kusto connection/cluster; the default connection is used when <see langword="null"/>.
    /// </param>
    /// <param name="databaseName">
    /// Optional database name; the default database is used when <see langword="null"/>.
    /// </param>
    /// <param name="options">
    /// Optional streaming-query options (timeouts, buffer size, etc.); default options are applied when <see langword="null"/>.
    /// </param>
    /// <returns>
    /// A streaming query handler whose <see cref="IAsyncEnumerable{T}"/> emits results in real time.
    /// </returns>
    IStreamingScriptHandler<T?> Create<T>(
        IKustoStreamingQuery<T> query,
        string? connectionName = null,
        string? databaseName = null,
        AtcStreamingQueryOptions? options = null);
}