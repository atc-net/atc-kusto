namespace Atc.Kusto.Tests.Utilities.Internal;

public sealed class KustoTransientErrorsTests
{
    public static TheoryData<Exception> PermanentErrors => new()
    {
        new SemanticException(),
        new SyntaxException(),
        new KustoBadRequestException(),
        new DatabaseNotFoundException(),
        new KustoRequestDeniedException(),
        new KustoClientRequestCanceledByUserException(),
        new OperationCanceledException(),
        new TaskCanceledException(),
        new JsonException(),
        new InvalidCastException(),
        new NotSupportedException(),
        new Azure.RequestFailedException((int)HttpStatusCode.BadRequest, "bad request"),
        new Azure.RequestFailedException((int)HttpStatusCode.Forbidden, "forbidden"),
        new InvalidOperationException(),
    };

    public static TheoryData<Exception> TransientErrors => new()
    {
        new KustoServiceException(),
        new KustoRequestThrottledException(),
        new KustoServiceTimeoutException(),
        new KustoServiceUnavilableException(),
        new KustoClientTimeoutException(),
        new KustoClientUnableToConnectException(),
        new HttpRequestException(),
        new IOException(),
        new System.Net.Sockets.SocketException(),
        new TimeoutException(),
        new KustoClientAuthenticationException(),
        new Azure.Identity.AuthenticationFailedException("managed identity endpoint not ready"),
        new Azure.Identity.CredentialUnavailableException("managed identity unavailable"),
        new Azure.RequestFailedException("no response"), // Status 0: no response received
        new Azure.RequestFailedException((int)HttpStatusCode.TooManyRequests, "too many requests"),
        new Azure.RequestFailedException((int)HttpStatusCode.ServiceUnavailable, "service unavailable"),
    };

    [Theory]
    [MemberData(nameof(PermanentErrors))]
    public void IsTransient_Returns_False_For_Permanent_Errors(
        Exception exception)
        => KustoTransientErrors.IsTransient(exception).Should().BeFalse();

    [Theory]
    [MemberData(nameof(TransientErrors))]
    public void IsTransient_Returns_True_For_Transient_Errors(
        Exception exception)
        => KustoTransientErrors.IsTransient(exception).Should().BeTrue();

    [Fact]
    public void IsTransient_Returns_False_For_A_KustoServiceException_Marked_Permanent()
    {
        // Arrange - KustoServiceException is transient by default; the SDK marks this instance permanent
        var permanentServiceError = new KustoServiceException(
            errorCode: null,
            errorReason: null,
            errorMessage: "permanent",
            dataSource: null,
            databaseName: null,
            clientRequestId: null,
            activityId: Guid.Empty,
            failureCode: null,
            failureSubCode: null,
            isPermanent: true);

        // Act
        var isTransient = KustoTransientErrors.IsTransient(permanentServiceError);

        // Assert
        isTransient.Should().BeFalse();
    }
}