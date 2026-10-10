namespace Atc.Kusto.Handlers.Internal;

internal sealed partial class SimpleQueryHandler<T>
{
    private readonly ILogger<SimpleQueryHandler<T>> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SimpleQueryHandler.KustoServicePartialQueryFailureException,
        Level = LogLevel.Error,
        Message = "A partial query failure exception occurred for clientRequestId {ClientRequestId} and query {Query}")]
    private partial void LogKustoServicePartialQueryFailureException(
        Exception ex,
        string clientRequestId,
        string query);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SimpleQueryHandler.KustoServiceException,
        Level = LogLevel.Error,
        Message = "A Kusto service exception occurred for clientRequestId {ClientRequestId}")]
    private partial void LogKustoServiceException(
        Exception ex,
        string clientRequestId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SimpleQueryHandler.SemanticException,
        Level = LogLevel.Error,
        Message = "A semantic exception occurred for clientRequestId {ClientRequestId}. Query: {Text}. Errors: {SemanticErrors}")]
    private partial void LogSemanticException(
        Exception ex,
        string clientRequestId,
        string text,
        string semanticErrors);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SimpleQueryHandler.UnhandledException,
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred and some data might be lost")]
    private partial void LogUnhandledException(Exception ex);
}