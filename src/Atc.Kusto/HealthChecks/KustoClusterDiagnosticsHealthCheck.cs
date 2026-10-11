namespace Atc.Kusto.HealthChecks;

/// <summary>
/// Provides functionality to check the health of a Kusto (Azure Data Explorer) cluster.
/// </summary>
public sealed partial class KustoClusterDiagnosticsHealthCheck : IKustoClusterDiagnosticsHealthCheck
{
    private readonly IKustoProcessorFactory processorFactory;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoClusterDiagnosticsHealthCheck"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="processorFactory">The factory used to create Kusto processor instances.</param>
    /// <param name="timeProvider">
    /// The time provider that measures the duration of the check. Default is <see cref="TimeProvider.System"/>.
    /// </param>
    public KustoClusterDiagnosticsHealthCheck(
        ILogger<KustoClusterDiagnosticsHealthCheck> logger,
        IKustoProcessorFactory processorFactory,
        TimeProvider? timeProvider = null)
    {
        this.logger = logger;
        this.processorFactory = processorFactory;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<KustoHealthCheckResult> CheckHealthAsync(
        string? connectionName = null,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        var startTimestamp = timeProvider.GetTimestamp();

        try
        {
            var processor = processorFactory.Create(connectionName, databaseName);

            var clusterDiagnostics = await processor.ExecuteQuery(
                new KustoHealthCheckQuery(),
                cancellationToken: cancellationToken);

            var duration = timeProvider.GetElapsedTime(startTimestamp);

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
                    duration: duration);
            }

            var result = new KustoHealthCheckResult(
                isHealthy: clusterDiagnostics[0].IsHealthy == 1,
                notHealthyReason: clusterDiagnostics[0].NotHealthyReason,
                isAttentionRequired: clusterDiagnostics[0].IsAttentionRequired == 1,
                attentionRequiredReason: clusterDiagnostics[0].AttentionRequiredReason,
                isScaleOutRequired: clusterDiagnostics[0].IsScaleOutRequired == 1,
                duration: duration);

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
                LogHealthCheckSucceeded((long)duration.TotalMilliseconds);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var duration = timeProvider.GetElapsedTime(startTimestamp);
            LogHealthCheckException(ex, ex.Message);

            return new KustoHealthCheckResult(
                isHealthy: false,
                notHealthyReason: $"Exception during health check: {ex.Message}",
                isAttentionRequired: true,
                attentionRequiredReason: "Health check failed with exception",
                isScaleOutRequired: false,
                duration: duration);
        }
    }
}