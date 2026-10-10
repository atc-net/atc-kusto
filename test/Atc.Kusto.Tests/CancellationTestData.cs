namespace Atc.Kusto.Tests;

/// <summary>
/// Shared data for the cancellation tests.
/// </summary>
internal static class CancellationTestData
{
    /// <summary>
    /// Gets errors the Kusto SDK reports a cancelled request with, instead of an <see cref="OperationCanceledException"/>:
    /// its own cancel exception, and the service error a server-side <c>.cancel query</c> produces.
    /// </summary>
    public static TheoryData<Exception> SdkCancellationErrors => new()
    {
        new KustoClientRequestCanceledByUserException(),
        new KustoServiceException(),
    };

    /// <summary>
    /// Simulates an SDK call that fails because the caller cancelled while it ran:
    /// runs <paramref name="cancel"/> (e.g. <c>cts.CancelAsync</c>), then throws <paramref name="error"/>.
    /// </summary>
    /// <typeparam name="T">The result type of the simulated call.</typeparam>
    /// <param name="cancel">Cancels the caller's token.</param>
    /// <param name="error">The error the SDK reports.</param>
    /// <returns>A task that always fails with <paramref name="error"/>.</returns>
    public static async Task<T> CancelThenThrow<T>(
        Func<Task> cancel,
        Exception error)
    {
        await cancel();
        throw error;
    }
}