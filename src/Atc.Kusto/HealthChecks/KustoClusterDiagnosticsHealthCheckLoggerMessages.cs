namespace Atc.Kusto.HealthChecks;

public sealed partial class KustoClusterDiagnosticsHealthCheck
{
    private readonly ILogger<KustoClusterDiagnosticsHealthCheck> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoClusterDiagnosticsHealthCheck.NoDiagnosticDataReturned,
        Level = LogLevel.Warning,
        Message = "Health check response from Kusto cluster contained no diagnostic data")]
    private partial void LogNoDiagnosticDataReturned();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoClusterDiagnosticsHealthCheck.HealthCheckFailed,
        Level = LogLevel.Warning,
        Message = "Kusto cluster health check failed. Reason: {Reason}")]
    private partial void LogHealthCheckFailed(string? reason);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoClusterDiagnosticsHealthCheck.ClusterRequiresAttention,
        Level = LogLevel.Warning,
        Message = "Kusto cluster requires attention. Reason: {Reason}")]
    private partial void LogClusterRequiresAttention(string? reason);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoClusterDiagnosticsHealthCheck.ClusterScaleOutRecommended,
        Level = LogLevel.Warning,
        Message = "Kusto cluster scale out is recommended")]
    private partial void LogClusterScaleOutRecommended();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoClusterDiagnosticsHealthCheck.HealthCheckSucceeded,
        Level = LogLevel.Trace,
        Message = "Kusto cluster health check passed successfully in {DurationMs}ms")]
    private partial void LogHealthCheckSucceeded(long durationMs);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoClusterDiagnosticsHealthCheck.UnhandledException,
        Level = LogLevel.Error,
        Message = "Error performing Kusto health check: {ErrorMessage}")]
    private partial void LogHealthCheckException(
        Exception ex,
        string errorMessage);
}