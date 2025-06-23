namespace Atc.Kusto.Tests.HealthChecks;

public sealed class HealthCheckExtensionsTests
{
    [Theory, AutoNSubstituteData]
    internal void AddKustoClusterDiagnosticsHealthCheck_WithExplicitValues_RegistersCorrectly(
        string name,
        string connectionName,
        string databaseName,
        string[] tags,
        IKustoProcessorFactory processorFactory)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(processorFactory);
        services.AddHealthChecks();

        // Act
        services
            .AddHealthChecks()
            .AddKustoClusterDiagnosticsHealthCheck(
                name: name,
                connectionName: connectionName,
                databaseName: databaseName,
                failureStatus: HealthStatus.Unhealthy,
                tags: tags,
                timeout: TimeSpan.FromSeconds(30));

        using var provider = services.BuildServiceProvider();

        // Assert – registration metadata
        var registration = provider
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations
            .SingleOrDefault(r => r.Name == name);

        registration
            .Should()
            .NotBeNull();

        registration!.FailureStatus
            .Should()
            .Be(HealthStatus.Unhealthy);

        registration.Tags
            .Should()
            .BeEquivalentTo(tags);

        // Assert – factory produces expected type
        registration.Factory(provider)
            .Should()
            .BeOfType<KustoHealthCheckPublisher>();
    }

    [Theory, AutoNSubstituteData]
    internal void AddKustoClusterDiagnosticsHealthCheck_WithDefaults_RegistersCorrectly(
        IKustoProcessorFactory processorFactory)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(processorFactory);
        services.AddHealthChecks();

        // Act
        services.AddHealthChecks().AddKustoClusterDiagnosticsHealthCheck();

        using var provider = services.BuildServiceProvider();

        // Assert
        provider
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations
            .Should()
            .ContainSingle(r => r.Name == "kusto");
    }
}