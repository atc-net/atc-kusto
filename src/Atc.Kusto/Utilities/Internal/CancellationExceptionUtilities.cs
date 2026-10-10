namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Utilities for handling cancellation exceptions in Kusto operations.
/// </summary>
/// <remarks>
/// The Kusto SDK often reports a cancelled request with its own exceptions, such as
/// <c>KustoClientRequestCanceledByUserException</c>, or with a <see cref="KustoServiceException"/> once a
/// server-side <c>.cancel query</c> has stopped the query. Hosts like ASP.NET Core only recognise
/// <see cref="OperationCanceledException"/> as cancellation, so these are translated whenever the
/// caller's token was cancelled.
/// </remarks>
internal static class CancellationExceptionUtilities
{
    /// <summary>
    /// Determines whether a failure must be reported as cancellation.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> for any exception once <paramref name="cancellationToken"/> is cancelled,
    /// whatever its type, and for an <see cref="OperationCanceledException"/> in any case. A Kusto
    /// cancel exception raised while the token is not cancelled (e.g. someone ran <c>.cancel query</c>)
    /// is not cancellation by the caller and is handled as an ordinary failure.
    /// </remarks>
    /// <param name="exception">The exception to check.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>True if the failure must surface as <see cref="OperationCanceledException"/>; otherwise, false.</returns>
    public static bool IsCancellation(
        Exception exception,
        CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested
           || IsOperationCanceled(exception);

    /// <summary>
    /// Normalizes cancellation exceptions to standard OperationCanceledException.
    /// </summary>
    /// <remarks>
    /// An existing <see cref="OperationCanceledException"/> is returned unchanged; any other exception
    /// becomes the <see cref="Exception.InnerException"/> of a new one carrying <paramref name="cancellationToken"/>.
    /// </remarks>
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

    /// <summary>
    /// Determines whether an exception is an <see cref="OperationCanceledException"/>, including
    /// <see cref="TaskCanceledException"/>, which derives from it.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is an <see cref="OperationCanceledException"/>; otherwise, false.</returns>
    private static bool IsOperationCanceled(Exception exception)
        => exception is OperationCanceledException;
}