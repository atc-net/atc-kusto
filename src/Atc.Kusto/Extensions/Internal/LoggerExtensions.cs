namespace Atc.Kusto.Extensions.Internal;

public static partial class LoggerExtensions
{
    [LoggerMessage(
        EventId = LoggingEventIdConstants.CancellationTokenKustoExtensions.KustoCancelCommandFailed,
        Level = LogLevel.Error,
        Message = "Kusto cancel command failed")]
    public static partial void LogKustoCancelCommandFailed(
        this ILogger logger,
        Exception ex);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.CancellationTokenKustoExtensions.FailedToScheduleKustoCancel,
        Level = LogLevel.Error,
        Message = "Failed to schedule Kusto cancel")]
    public static partial void LogFailedToScheduleKustoCancel(
        this ILogger logger,
        Exception ex);
}