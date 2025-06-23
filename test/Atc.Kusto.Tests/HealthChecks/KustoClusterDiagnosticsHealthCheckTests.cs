namespace Atc.Kusto.Tests.HealthChecks;

public sealed class KustoClusterDiagnosticsHealthCheckTests
{
    [Theory, AutoNSubstituteData]
    internal async Task CheckHealthAsync_Healthy_ReturnsHealthy(
        [Frozen] IKustoProcessorFactory factory,
        [Frozen] IKustoProcessor processor,
        string connectionName,
        string databaseName,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(connectionName, databaseName)
            .Returns(processor);

        var diagnostics = new[]
        {
            new KustoClusterDiagnostics
            {
                IsHealthy = 1,
                IsAttentionRequired = 0,
                IsScaleOutRequired = 0,
            },
        };

        processor
            .ExecuteQuery(
                Arg.Any<KustoHealthCheckQuery>(),
                cancellationToken)
            .Returns(Task.FromResult(diagnostics));

        var sut = new KustoClusterDiagnosticsHealthCheck(
            NullLogger<KustoClusterDiagnosticsHealthCheck>.Instance,
            factory);

        // Act
        var result = await sut.CheckHealthAsync(connectionName, databaseName, cancellationToken);

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.IsAttentionRequired.Should().BeFalse();
        result.IsScaleOutRequired.Should().BeFalse();
    }

    [Theory, AutoNSubstituteData]
    internal async Task CheckHealthAsync_Unhealthy_ReturnsUnhealthy(
        [Frozen] IKustoProcessorFactory factory,
        [Frozen] IKustoProcessor processor,
        string connectionName,
        string databaseName,
        string notHealthyReason,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(connectionName, databaseName)
            .Returns(processor);

        var diagnostics = new[]
        {
            new KustoClusterDiagnostics
            {
                IsHealthy = 0,
                NotHealthyReason = notHealthyReason,
                IsAttentionRequired = 1,
                AttentionRequiredReason = "Attention needed",
                IsScaleOutRequired = 0,
            },
        };

        processor
            .ExecuteQuery(
                Arg.Any<KustoHealthCheckQuery>(),
                cancellationToken)
            .Returns(Task.FromResult(diagnostics));

        var sut = new KustoClusterDiagnosticsHealthCheck(
            NullLogger<KustoClusterDiagnosticsHealthCheck>.Instance,
            factory);

        // Act
        var result = await sut.CheckHealthAsync(connectionName, databaseName, cancellationToken);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.NotHealthyReason.Should().Be(notHealthyReason);
        result.IsAttentionRequired.Should().BeTrue();
        result.AttentionRequiredReason.Should().Be("Attention needed");
        result.IsScaleOutRequired.Should().BeFalse();
    }

    [Theory, AutoNSubstituteData]
    internal async Task CheckHealthAsync_Exception_ReturnsUnhealthy(
        [Frozen] IKustoProcessorFactory factory,
        [Frozen] IKustoProcessor processor,
        string connectionName,
        string databaseName,
        string exceptionMessage,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(connectionName, databaseName)
            .Returns(processor);

        processor
            .ExecuteQuery(
                Arg.Any<KustoHealthCheckQuery>(),
                cancellationToken)
            .Returns<Task<KustoClusterDiagnostics[]>>(_ => throw new InvalidOperationException(exceptionMessage));

        var sut = new KustoClusterDiagnosticsHealthCheck(
            NullLogger<KustoClusterDiagnosticsHealthCheck>.Instance,
            factory);

        // Act
        var result = await sut.CheckHealthAsync(connectionName, databaseName, cancellationToken);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.NotHealthyReason.Should().Contain(exceptionMessage);
        result.IsAttentionRequired.Should().BeTrue();
        result.AttentionRequiredReason.Should().Be("Health check failed with exception");
    }

    [Theory, AutoNSubstituteData]
    internal async Task CheckHealthAsync_NoDiagnostics_ReturnsUnhealthy(
        [Frozen] IKustoProcessorFactory factory,
        [Frozen] IKustoProcessor processor,
        string connectionName,
        string databaseName,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(connectionName, databaseName)
            .Returns(processor);

        processor
            .ExecuteQuery(
                Arg.Any<KustoHealthCheckQuery>(),
                cancellationToken)
            .Returns(Task.FromResult(Array.Empty<KustoClusterDiagnostics>()));

        var sut = new KustoClusterDiagnosticsHealthCheck(
            NullLogger<KustoClusterDiagnosticsHealthCheck>.Instance,
            factory);

        // Act
        var result = await sut.CheckHealthAsync(connectionName, databaseName, cancellationToken);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.NotHealthyReason.Should().Be("No diagnostic data returned from cluster");
        result.IsAttentionRequired.Should().BeTrue();
        result.AttentionRequiredReason.Should().Be("No diagnostic data returned from cluster");
    }

    internal async Task CheckHealthAsync_ScaleOutRequired_ReturnsScaleOutTrue(
        [Frozen] IKustoProcessorFactory factory,
        [Frozen] IKustoProcessor processor,
        string connectionName,
        string databaseName,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(connectionName, databaseName)
            .Returns(processor);

        var diagnostics = new[]
        {
            new KustoClusterDiagnostics
            {
                IsHealthy = 1,
                IsAttentionRequired = 0,
                IsScaleOutRequired = 1,
            },
        };

        processor
            .ExecuteQuery(
                Arg.Any<KustoHealthCheckQuery>(),
                cancellationToken)
            .Returns(Task.FromResult(diagnostics));

        var sut = new KustoClusterDiagnosticsHealthCheck(
            NullLogger<KustoClusterDiagnosticsHealthCheck>.Instance,
            factory);

        // Act
        var result = await sut.CheckHealthAsync(connectionName, databaseName, cancellationToken);

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.IsScaleOutRequired.Should().BeTrue();
        result.IsAttentionRequired.Should().BeFalse();
    }

    [Theory, AutoNSubstituteData]
    internal async Task CheckHealthAsync_AttentionRequired_ReturnsAttentionTrue(
        [Frozen] IKustoProcessorFactory factory,
        [Frozen] IKustoProcessor processor,
        string connectionName,
        string databaseName,
        string attentionReason,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(connectionName, databaseName)
            .Returns(processor);

        var diagnostics = new[]
        {
            new KustoClusterDiagnostics
            {
                IsHealthy = 1,
                IsAttentionRequired = 1,
                AttentionRequiredReason = attentionReason,
                IsScaleOutRequired = 0,
            },
        };

        processor
            .ExecuteQuery(
                Arg.Any<KustoHealthCheckQuery>(),
                cancellationToken)
            .Returns(Task.FromResult(diagnostics));

        var sut = new KustoClusterDiagnosticsHealthCheck(
            NullLogger<KustoClusterDiagnosticsHealthCheck>.Instance,
            factory);

        // Act
        var result = await sut.CheckHealthAsync(connectionName, databaseName, cancellationToken);

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.IsAttentionRequired.Should().BeTrue();
        result.AttentionRequiredReason.Should().Be(attentionReason);
        result.IsScaleOutRequired.Should().BeFalse();
    }
}