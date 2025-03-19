namespace Atc.Kusto.Extensions.Internal;

internal static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Converts a synchronous <see cref="IEnumerator{T}"/> to an asynchronous <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerator.</typeparam>
    /// <param name="enumerator">The synchronous enumerator to convert.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous enumerable that yields elements from the enumerator.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is canceled.</exception>
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IEnumerator<T> enumerator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(enumerator);
        return ToAsyncEnumerableIterator(enumerator, cancellationToken);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerableIterator<T>(
        IEnumerator<T> enumerator,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (enumerator.MoveNext())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return enumerator.Current;
            await Task.Yield();
        }
    }
}