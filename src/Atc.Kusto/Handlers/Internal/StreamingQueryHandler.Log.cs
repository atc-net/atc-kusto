namespace Atc.Kusto.Handlers.Internal;

internal sealed partial class StreamingQueryHandler<T>
{
    private readonly ILogger<StreamingQueryHandler<T>> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.StreamingQueryHandler.ReceivedTableFragmentForUnknownTable,
        Level = LogLevel.Warning,
        Message = "Received table fragment for unknown table with tableId '{TableId}'")]
    private partial void LogReceivedTableFragmentForUnknownTable(int tableId);
}