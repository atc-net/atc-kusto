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

    /// <summary>
    /// Wraps an async enumerable to normalize cancellation exceptions to standard OperationCanceledException.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source async enumerable.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable that normalizes cancellation exceptions.</returns>
    public static IAsyncEnumerable<T> NormalizeCancellationExceptions<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        return NormalizeCancellationExceptionsIterator(source, cancellationToken);
    }

    private static async IAsyncEnumerable<T> NormalizeCancellationExceptionsIterator<T>(
        IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerator<T>? enumerator = null;

        try
        {
            enumerator = source.GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (CancellationExceptionUtilities.IsCancellationException(ex))
                {
                    throw CancellationExceptionUtilities.NormalizeCancellationException(ex, cancellationToken);
                }

                if (!hasNext)
                {
                    break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            if (enumerator is not null)
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}