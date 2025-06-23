namespace Atc.Kusto.HealthChecks;

/// <summary>
/// Extension methods for adding Kusto health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds a health check for the Kusto (Azure Data Explorer) cluster to the health check service.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/> to add the health check to.</param>
    /// <param name="name">The health check name. Default is "kusto".</param>
    /// <param name="connectionName">
    /// Optional connection name that identifies the Kusto cluster to check.
    /// If <see langword="null"/>, the default connection from configuration is used.
    /// </param>
    /// <param name="databaseName">
    /// Optional database name for the health check operation.
    /// If <see langword="null"/>, the default database name from the connection is used.
    /// </param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails.
    /// Default is <see cref="HealthStatus.Unhealthy"/>.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <param name="timeout">A <see cref="TimeSpan"/> representing the timeout of the health check.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> so that additional calls can be chained.</returns>
    public static IHealthChecksBuilder AddKustoClusterDiagnosticsHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "kusto",
        string? connectionName = null,
        string? databaseName = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(
            new HealthCheckRegistration(
                name,
                sp => new KustoHealthCheckPublisher(
                    new KustoClusterDiagnosticsHealthCheck(
                        sp.GetService<ILogger<KustoClusterDiagnosticsHealthCheck>>() ?? NullLogger<KustoClusterDiagnosticsHealthCheck>.Instance,
                        sp.GetRequiredService<IKustoProcessorFactory>()),
                    connectionName,
                    databaseName),
                failureStatus,
                tags,
                timeout));
    }
}