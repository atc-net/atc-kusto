namespace Atc.Kusto.Ingestion.Internal;

internal sealed partial class KustoIngestor
{
    private readonly ILogger<KustoIngestor> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoIngestor.Started,
        Level = LogLevel.Debug,
        Message = "Ingestion started: database={Database}, table={Table}, mode={Mode}")]
    private partial void LogIngestionStarted(
        string database,
        string table,
        IngestionMode mode);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoIngestor.Succeeded,
        Level = LogLevel.Information,
        Message = "Ingestion succeeded: database={Database}, table={Table}, mode={Mode}, operationId={OperationId}, durationMs={DurationMs}")]
    private partial void LogIngestionSucceeded(
        string database,
        string table,
        IngestionMode mode,
        string? operationId,
        double durationMs);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoIngestor.Queued,
        Level = LogLevel.Information,
        Message = "Ingestion queued: database={Database}, table={Table}, mode={Mode}, operationId={OperationId}, durationMs={DurationMs}")]
    private partial void LogIngestionQueued(
        string database,
        string table,
        IngestionMode mode,
        string? operationId,
        double durationMs);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoIngestor.Skipped,
        Level = LogLevel.Debug,
        Message = "Ingestion skipped, input was empty: table={Table}, mode={Mode}")]
    private partial void LogIngestionSkipped(
        string table,
        IngestionMode mode);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.KustoIngestor.Failed,
        Level = LogLevel.Error,
        Message = "Ingestion failed: database={Database}, table={Table}, mode={Mode}. {ErrorMessage}")]
    private partial void LogIngestionFailed(
        Exception ex,
        string database,
        string table,
        IngestionMode mode,
        string errorMessage);
}