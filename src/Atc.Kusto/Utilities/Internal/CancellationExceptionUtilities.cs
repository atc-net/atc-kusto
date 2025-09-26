namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Utilities for handling cancellation exceptions in Kusto operations.
/// </summary>
internal static class CancellationExceptionUtilities
{
    /// <summary>
    /// Determines if an exception is a cancellation exception (including Kusto-specific ones).
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception represents a cancellation; otherwise, false.</returns>
    public static bool IsCancellationException(Exception exception)
        => exception is OperationCanceledException
           || exception is TaskCanceledException;

    /// <summary>
    /// Normalizes cancellation exceptions to standard OperationCanceledException.
    /// </summary>
    /// <param name="exception">The exception to normalize.</param>
    /// <param name="cancellationToken">The cancellation token associated with the operation.</param>
    /// <returns>A normalized OperationCanceledException.</returns>
    public static OperationCanceledException NormalizeCancellationException(
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException oce)
        {
            return oce;
        }

        return new OperationCanceledException(
            $"The operation was canceled. Original exception: {exception.GetType().Name}",
            exception,
            cancellationToken);
    }
}