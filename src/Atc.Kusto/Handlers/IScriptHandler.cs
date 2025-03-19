namespace Atc.Kusto.Handlers;

/// <summary>
/// Defines a handler for executing a script that returns a result of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the result returned by the script.</typeparam>
public interface IScriptHandler<T>
{
    /// <summary>
    /// Executes the script asynchronously and returns the result of type <typeparamref name="T"/> if available.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains the script's result of type <typeparamref name="T"/> or null if the result is not available.</returns>
    Task<T?> Execute(CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for executing a script that does not return a result.
/// </summary>
public interface IScriptHandler
{
    /// <summary>
    /// Executes the script asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Execute(CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for executing a script that streams results of type <typeparamref name="T"/> as they become available.
/// </summary>
/// <typeparam name="T">The type of the results streamed by the script.</typeparam>
public interface IStreamingScriptHandler<out T>
{
    /// <summary>
    /// Executes the script asynchronously and streams results of type <typeparamref name="T"/> as they become available.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous stream of results of type <typeparamref name="T"/> that can be enumerated as they are produced.</returns>
    IAsyncEnumerable<T?> Execute(CancellationToken cancellationToken);
}