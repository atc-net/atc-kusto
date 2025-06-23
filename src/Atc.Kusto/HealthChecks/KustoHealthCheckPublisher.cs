namespace Atc.Kusto.HealthChecks;

/// <summary>
/// Implements a health check for a Kusto (Azure Data Explorer) cluster.
/// </summary>
internal class KustoHealthCheckPublisher : IHealthCheck
{
    private readonly IKustoClusterDiagnosticsHealthCheck healthCheck;
    private readonly string? connectionName;
    private readonly string? databaseName;

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoHealthCheckPublisher"/> class.
    /// </summary>
    /// <param name="healthCheck">The Kusto health check service.</param>
    /// <param name="connectionName">Optional connection name that identifies the Kusto cluster to check.</param>
    /// <param name="databaseName">Optional database name for the health check operation.</param>
    public KustoHealthCheckPublisher(
        IKustoClusterDiagnosticsHealthCheck healthCheck,
        string? connectionName = null,
        string? databaseName = null)
    {
        this.healthCheck = healthCheck;
        this.connectionName = connectionName;
        this.databaseName = databaseName;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await healthCheck.CheckHealthAsync(
                connectionName,
                databaseName,
                cancellationToken);

            var data = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["Duration"] = result.Duration,
                ["IsAttentionRequired"] = result.IsAttentionRequired,
                ["IsScaleOutRequired"] = result.IsScaleOutRequired,
            };

            if (result.NotHealthyReason is not null)
            {
                data["NotHealthyReason"] = result.NotHealthyReason;
            }

            if (result.AttentionRequiredReason is not null)
            {
                data["AttentionRequiredReason"] = result.AttentionRequiredReason;
            }

            if (!result.IsHealthy)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    description: $"Kusto cluster is not healthy: {result.NotHealthyReason}",
                    data: data);
            }

            if (result.IsAttentionRequired)
            {
                return new HealthCheckResult(
                    HealthStatus.Degraded,
                    description: $"Kusto cluster requires attention: {result.AttentionRequiredReason}",
                    data: data);
            }

            return new HealthCheckResult(
                HealthStatus.Healthy,
                description: "Kusto cluster is healthy",
                data: data);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                description: $"Kusto health check failed with exception: {ex.Message}",
                exception: ex);
        }
    }
}