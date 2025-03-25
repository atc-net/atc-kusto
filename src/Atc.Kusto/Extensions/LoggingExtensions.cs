namespace Atc.Kusto.Extensions;

internal static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = LoggingEventIdConstants.ResiliencePipeline.Retry,
        Level = LogLevel.Warning,
        Message = "{ErrorMessage}; Delaying for {DelaySeconds} seconds, then making retry {RetryAttemptNumber} of {MaxRetryAttempts}")]
    public static partial void LogRetryWarning(
        this ILogger logger,
        string errorMessage,
        double delaySeconds,
        int retryAttemptNumber,
        int maxRetryAttempts);
}