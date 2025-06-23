namespace Atc.Kusto.HealthChecks;

/// <summary>
/// Provides functionality to check the health of a Kusto (Azure Data Explorer) cluster.
/// </summary>
public interface IKustoClusterDiagnosticsHealthCheck
{
    /// <summary>
    /// Checks the health of the Kusto cluster asynchronously.
    /// </summary>
    /// <param name="connectionName">
    /// Optional connection name that identifies the Kusto cluster to check.
    /// If <see langword="null"/>, the default connection is utilized.
    /// </param>
    /// <param name="databaseName">
    /// Optional database name for the health check operation.
    /// If <see langword="null"/>, the default database name from the connection is used.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with a <see cref="KustoHealthCheckResult"/> containing health status information.</returns>
    Task<KustoHealthCheckResult> CheckHealthAsync(
        string? connectionName = null,
        string? databaseName = null,
        CancellationToken cancellationToken = default);
}