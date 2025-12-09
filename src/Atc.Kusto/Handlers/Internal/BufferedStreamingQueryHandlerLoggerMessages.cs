namespace Atc.Kusto.Handlers.Internal;

internal sealed partial class BufferedStreamingQueryHandler<T>
{
    private readonly ILogger<BufferedStreamingQueryHandler<T>> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BufferedStreamingQueryHandler.ProgressiveDataSetIsNull,
        Level = LogLevel.Error,
        Message = "ExecuteQueryV2Async returned a null ProgressiveDataSet for query {Query} with clientRequestId {ClientRequestId}")]
    private partial void LogProgressiveDataSetIsNull(
        string query,
        string clientRequestId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BufferedStreamingQueryHandler.UnhandledException,
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred for clientRequestId {ClientRequestId} and query {Query}")]
    private partial void LogUnhandledException(
        Exception ex,
        string clientRequestId,
        string query);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BufferedStreamingQueryHandler.SchemaNullInDataTableDataSetFrame,
        Level = LogLevel.Warning,
        Message = "Returned schema in tableData was null for tableId '{TableId}', tableName '{TableName}' with tableKind '{tableKind}'")]
    private partial void LogSchemaNullInDataTableDataSetFrame(
        int tableId,
        string tableName,
        WellKnownDataSet tableKind);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BufferedStreamingQueryHandler.ReceivedTableFragmentForUnknownTable,
        Level = LogLevel.Warning,
        Message = "Received table fragment for unknown table with tableId '{TableId}'")]
    private partial void LogReceivedTableFragmentForUnknownTable(int tableId);
}