namespace Atc.Kusto.Handlers.Internal;

internal sealed partial class ExistingPagedStoredQueryHandler<T>
{
    private readonly ILogger<ExistingPagedStoredQueryHandler<T>> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ExistingPagedStoredQueryHandler.KustoServicePartialQueryFailureException,
        Level = LogLevel.Error,
        Message = "An partial query failure exception occured for clientRequestId {ClientRequestId} and query {Query}")]
    private partial void LogKustoServicePartialQueryFailureException(
        Exception ex,
        string clientRequestId,
        string query);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ExistingPagedStoredQueryHandler.KustoServiceException,
        Level = LogLevel.Error,
        Message = "An kusto service exception occured for clientRequestId {ClientRequestId}")]
    private partial void LogKustoServiceException(
        Exception ex,
        string clientRequestId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ExistingPagedStoredQueryHandler.SemanticException,
        Level = LogLevel.Error,
        Message = "An semantic exception occured for clientRequestId {ClientRequestId}")]
    private partial void LogSemanticException(
        Exception ex,
        string clientRequestId,
        string text,
        string semanticErrors);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ExistingPagedStoredQueryHandler.UnhandledException,
        Level = LogLevel.Error,
        Message = "An unhandled exception occured and some data might be lost")]
    private partial void LogUnhandledException(Exception ex);
}