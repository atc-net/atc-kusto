namespace Atc.Kusto.Handlers.Internal;

internal sealed partial class ExistingPagedStoredQueryHandler<T>
{
    private readonly ILogger<ExistingPagedStoredQueryHandler<T>> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ExistingPagedStoredQueryHandler.StoredQueryResultNotFound,
        Level = LogLevel.Information,
        Message = "The stored query result {QueryId} was not found (it has expired), so the query runs again")]
    private partial void LogStoredQueryResultNotFound(string queryId);
}