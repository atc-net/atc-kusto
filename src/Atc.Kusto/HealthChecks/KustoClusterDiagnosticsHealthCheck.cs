namespace Atc.Kusto.HealthChecks;

/// <summary>
/// Provides functionality to check the health of a Kusto (Azure Data Explorer) cluster.
/// </summary>
public sealed partial class KustoClusterDiagnosticsHealthCheck : IKustoClusterDiagnosticsHealthCheck
{
    private readonly IKustoProcessorFactory processorFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoClusterDiagnosticsHealthCheck"/> class.
    /// </summary>
    /// <param name="processorFactory">The factory used to create Kusto processor instances.</param>
    /// <param name="logger">The logger instance.</param>
    public KustoClusterDiagnosticsHealthCheck(
        ILogger<KustoClusterDiagnosticsHealthCheck> logger,
        IKustoProcessorFactory processorFactory)
    {
        this.logger = logger;
        this.processorFactory = processorFactory;
    }

    /// <inheritdoc />
    public async Task<KustoHealthCheckResult> CheckHealthAsync(
        string? connectionName = null,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var processor = processorFactory.Create(connectionName, databaseName);

            var clusterDiagnostics = await processor.ExecuteQuery(
                new KustoHealthCheckQuery(),
                cancellationToken: cancellationToken);

            stopwatch.Stop();

            if (clusterDiagnostics is null ||
                clusterDiagnostics.Length == 0)
            {
                LogNoDiagnosticDataReturned();
                return new KustoHealthCheckResult(
                    isHealthy: false,
                    notHealthyReason: "No diagnostic data returned from cluster",
                    isAttentionRequired: true,
                    attentionRequiredReason: "No diagnostic data returned from cluster",
                    isScaleOutRequired: false,
                    duration: stopwatch.Elapsed);
            }

            var result = new KustoHealthCheckResult(
                isHealthy: clusterDiagnostics[0].IsHealthy == 1,
                notHealthyReason: clusterDiagnostics[0].NotHealthyReason,
                isAttentionRequired: clusterDiagnostics[0].IsAttentionRequired == 1,
                attentionRequiredReason: clusterDiagnostics[0].AttentionRequiredReason,
                isScaleOutRequired: clusterDiagnostics[0].IsScaleOutRequired == 1,
                duration: stopwatch.Elapsed);

            if (!result.IsHealthy)
            {
                LogHealthCheckFailed(result.NotHealthyReason);
            }
            else if (result.IsAttentionRequired)
            {
                LogClusterRequiresAttention(result.AttentionRequiredReason);
            }
            else if (result.IsScaleOutRequired)
            {
                LogClusterScaleOutRecommended();
            }
            else
            {
                LogHealthCheckSucceeded(stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogHealthCheckException(ex, ex.Message);

            return new KustoHealthCheckResult(
                isHealthy: false,
                notHealthyReason: $"Exception during health check: {ex.Message}",
                isAttentionRequired: true,
                attentionRequiredReason: "Health check failed with exception",
                isScaleOutRequired: false,
                duration: stopwatch.Elapsed);
        }
    }
}