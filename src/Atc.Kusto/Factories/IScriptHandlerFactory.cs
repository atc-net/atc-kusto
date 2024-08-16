namespace Atc.Kusto.Factories;

/// <summary>
/// Factory interface for creating script handlers that can execute various types of Kusto scripts and queries.
/// </summary>
public interface IScriptHandlerFactory
{
    /// <summary>
    /// Creates an <see cref="IScriptHandler"/> for executing a Kusto command.
    /// </summary>
    /// <param name="command">The Kusto command to be executed.</param>
    /// <returns>An instance of <see cref="IScriptHandler"/> capable of executing the command.</returns>
    IScriptHandler Create(
        IKustoCommand command);

    /// <summary>
    /// Creates an <see cref="IScriptHandler{T}"/> for executing a Kusto query that returns a result of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the query.</typeparam>
    /// <param name="query">The Kusto query to be executed.</param>
    /// <returns>An instance of <see cref="IScriptHandler{T}"/> capable of executing the query and returning the result.</returns>
    IScriptHandler<T> Create<T>(
        IKustoQuery<T> query);

    /// <summary>
    /// Creates an <see cref="IScriptHandler{T}"/> for executing a Kusto query that returns a paginated result set.
    /// </summary>
    /// <typeparam name="T">The type of items in the result set.</typeparam>
    /// <param name="query">The Kusto query to be executed.</param>
    /// <param name="sessionId">An optional session ID for tracking the query execution.</param>
    /// <param name="pageSize">The number of items per page in the result set.</param>
    /// <param name="continuationToken">An optional token to continue fetching results from a previous query execution.</param>
    /// <returns>An instance of <see cref="IScriptHandler{T}"/> capable of executing the query and returning a paginated result.</returns>
    IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int pageSize,
        string? continuationToken);
}