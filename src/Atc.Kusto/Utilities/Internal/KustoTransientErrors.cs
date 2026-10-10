namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Decides whether a failed query or command is worth retrying.
/// </summary>
internal static class KustoTransientErrors
{
    /// <summary>
    /// The <see cref="Azure.RequestFailedException.Status"/> Azure.Core reports when no response was received.
    /// </summary>
    private const int NoResponseStatus = 0;

    /// <summary>
    /// Determines whether a failed query or command may succeed when retried.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Kusto SDK exceptions carry their own classification in <see cref="KustoException.IsPermanent"/>:
    /// syntax and semantic errors, missing databases or tables and denied requests are permanent;
    /// throttling, timeouts and an unavailable service are not.
    /// </para>
    /// <para>
    /// Sign-in failures are always retried, although the SDK marks its own as permanent: a temporary
    /// failure (e.g. a managed identity endpoint not yet ready after a deploy) cannot be told apart from
    /// a misconfiguration. With a <c>TokenCredential</c> the SDK passes the Azure.Identity exception
    /// through unwrapped; an <see cref="Azure.RequestFailedException"/> from a token endpoint is retried
    /// only for a status worth retrying.
    /// </para>
    /// <para>
    /// Any other exception is retried only when it is a network failure. Everything else, such as a
    /// result that cannot be mapped, fails the same way on every attempt.
    /// </para>
    /// </remarks>
    /// <param name="exception">The exception the attempt failed with.</param>
    /// <returns><see langword="true"/> when a retry may succeed; otherwise <see langword="false"/>.</returns>
    public static bool IsTransient(Exception exception)
        => exception switch
        {
            OperationCanceledException => false,
            KustoClientAuthenticationException => true,
            KustoException kustoException => !kustoException.IsPermanent,
            Azure.Identity.AuthenticationFailedException => true,
            Azure.RequestFailedException requestFailed => IsTransientStatus(requestFailed.Status),
            HttpRequestException or IOException or SocketException or TimeoutException => true,
            _ => false,
        };

    /// <summary>
    /// Determines whether an HTTP status may succeed when retried.
    /// </summary>
    private static bool IsTransientStatus(int status)
        => status is NoResponseStatus
            or (int)HttpStatusCode.RequestTimeout
            or (int)HttpStatusCode.TooManyRequests
            or >= (int)HttpStatusCode.InternalServerError;
}