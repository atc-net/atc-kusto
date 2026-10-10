namespace Atc.Kusto.Handlers.Internal;

internal sealed partial class StreamingQueryHandler<T>
{
    private readonly ILogger<StreamingQueryHandler<T>> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.StreamingQueryHandler.SchemaNullInDataTableDataSetFrame,
        Level = LogLevel.Warning,
        Message = "Returned schema in tableData was null for tableId '{TableId}', tableName '{TableName}' with tableKind '{tableKind}'")]
    private partial void LogSchemaNullInDataTableDataSetFrame(
        int tableId,
        string tableName,
        WellKnownDataSet tableKind);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.StreamingQueryHandler.ReceivedTableFragmentForUnknownTable,
        Level = LogLevel.Warning,
        Message = "Received table fragment for unknown table with tableId '{TableId}'")]
    private partial void LogReceivedTableFragmentForUnknownTable(int tableId);
}