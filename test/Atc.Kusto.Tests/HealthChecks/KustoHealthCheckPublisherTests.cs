namespace Atc.Kusto.Tests.HealthChecks;

public sealed class KustoHealthCheckPublisherTests
{
    [Theory, AutoNSubstituteData]
    internal async Task CheckHealthAsync_Lets_Cancellation_Through_When_The_Caller_Cancelled(
        [Frozen] IKustoClusterDiagnosticsHealthCheck healthCheck,
        KustoHealthCheckPublisher sut)
    {
        // Arrange - the health check service reports a timeout itself and stops quietly on shutdown
        using var cts = new CancellationTokenSource();

        healthCheck
            .CheckHealthAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CancellationTestData.CancelThenThrow<KustoHealthCheckResult>(
                cts.CancelAsync,
                new OperationCanceledException(cts.Token)));

        // Act
        var act = () => sut.CheckHealthAsync(CreateContext(sut), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory, AutoNSubstituteData]
    internal async Task CheckHealthAsync_Reports_Failure_For_A_Cancellation_The_Caller_Did_Not_Request(
        [Frozen] IKustoClusterDiagnosticsHealthCheck healthCheck,
        KustoHealthCheckPublisher sut)
    {
        // Arrange
        healthCheck
            .CheckHealthAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timeout"));

        // Act
        var result = await sut.CheckHealthAsync(CreateContext(sut), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    private static HealthCheckContext CreateContext(IHealthCheck healthCheck)
        => new()
        {
            Registration = new HealthCheckRegistration(
                "adx",
                healthCheck,
                HealthStatus.Unhealthy,
                tags: null),
        };
}