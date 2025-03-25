namespace Atc.Kusto.Handlers.Internal;

internal sealed partial class SimpleQueryHandler<T>
{
    private readonly ILogger<SimpleQueryHandler<T>> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SimpleQueryHandler.KustoServicePartialQueryFailureException,
        Level = LogLevel.Error,
        Message = "An partial query failure exception occured for clientRequestId {ClientRequestId} and query {Query}")]
    private partial void LogKustoServicePartialQueryFailureException(
        Exception ex,
        string clientRequestId,
        string query);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SimpleQueryHandler.KustoServiceException,
        Level = LogLevel.Error,
        Message = "An kusto service exception occured for clientRequestId {ClientRequestId}")]
    private partial void LogKustoServiceException(
        Exception ex,
        string clientRequestId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SimpleQueryHandler.SemanticException,
        Level = LogLevel.Error,
        Message = "An semantic exception occured for clientRequestId {ClientRequestId}")]
    private partial void LogSemanticException(
        Exception ex,
        string clientRequestId,
        string text,
        string semanticErrors);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SimpleQueryHandler.UnhandledException,
        Level = LogLevel.Error,
        Message = "An unhandled exception occured and some data might be lost")]
    private partial void LogUnhandledException(Exception ex);
}