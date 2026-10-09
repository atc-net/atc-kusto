namespace Atc.Kusto.Ingestion.Sample;

/// <summary>
/// One method per ingestion capability. Every row carries the run id and the scenario name, so each
/// scenario can read back exactly the rows it wrote.
/// </summary>
public sealed class IngestionScenarios
{
    private const string TableName = "SampleDeviceReadings";
    private const string MappingName = "SampleDeviceReadings_mapping";

    private readonly IKustoIngestor ingestor;
    private readonly IKustoProcessor processor;
    private readonly ILogger<IngestionScenarios> logger;
    private readonly string runId;
    private readonly Uri? blobUri;

    public IngestionScenarios(
        IKustoIngestor ingestor,
        IKustoProcessor processor,
        ILogger<IngestionScenarios> logger,
        string runId,
        Uri? blobUri)
    {
        this.ingestor = ingestor;
        this.processor = processor;
        this.logger = logger;
        this.runId = runId;
        this.blobUri = blobUri;
    }

    public async Task RunAllAsync(CancellationToken cancellationToken)
    {
        await ManagedStreamingByDefaultAsync(cancellationToken);
        await StreamingAndReadBackAsync(cancellationToken);
        await QueuedWithTrackingAsync(cancellationToken);
        await CsvStreamAsync(cancellationToken);
        await EmptyBatchIsSkippedAsync(cancellationToken);
        await MappingMismatchLeavesColumnsEmptyAsync(cancellationToken);
        await FailuresAreReturnedNotThrownAsync(cancellationToken);
        await BlobAsync(cancellationToken);
    }

    /// <summary>
    /// The default mode: streamed when possible (Succeeded), otherwise falls back to queued (Queued).
    /// </summary>
    private async Task ManagedStreamingByDefaultAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("--- 1. In-memory rows, default mode (ManagedStreaming)");

        var result = await ingestor.IngestAsync(
            Readings("managed", count: 3),
            JsonTarget(),
            cancellationToken: cancellationToken);

        result.EnsureSuccess();
        logger.LogInformation("Status {Status} (mode {Mode}), operation {OperationId}", result.Status, result.Mode, result.OperationId);
    }

    /// <summary>
    /// Pure streaming: when the call succeeds, the rows are already queryable.
    /// </summary>
    private async Task StreamingAndReadBackAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("--- 2. Streaming, then read the rows back immediately");

        var result = await ingestor.IngestAsync(
            Readings("streaming", count: 2),
            JsonTarget() with { Mode = IngestionMode.Streaming },
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Streaming failed (is streaming enabled on the cluster and table?): {Error}", result.ErrorMessage);
            return;
        }

        await LogRowsAsync("streaming", cancellationToken);
    }

    /// <summary>
    /// Queued ingestion is batched by the cluster; the rows become queryable later (typically minutes).
    /// </summary>
    private async Task QueuedWithTrackingAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("--- 3. Queued with tracking");

        var result = await ingestor.IngestAsync(
            Readings("queued", count: 5),
            JsonTarget() with { Mode = IngestionMode.Queued, EnableTracking = true },
            cancellationToken: cancellationToken);

        result.EnsureSuccess();
        logger.LogInformation(
            "Status {Status}; store the handle to check the outcome later. Operation {OperationId}, handle {OperationHandle}",
            result.Status,
            result.OperationId,
            result.OperationHandle);
    }

    /// <summary>
    /// A caller-owned stream in CSV format. CSV needs no mapping: columns are matched by position.
    /// </summary>
    private async Task CsvStreamAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("--- 4. CSV from a stream (no mapping, columns by position)");

        var csv = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{runId},csv,device-csv-1,SN-1001,21.5,{DateTime.UtcNow:O}")
            .AppendLine(CultureInfo.InvariantCulture, $"{runId},csv,device-csv-2,SN-1002,22.0,{DateTime.UtcNow:O}")
            .ToString();

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await ingestor.IngestAsync(
            stream,
            new KustoIngestTarget { TableName = TableName, Format = KustoIngestFormat.Csv },
            cancellationToken);

        result.EnsureSuccess();
        logger.LogInformation("Status {Status}; the stream is still open: {CanRead}", result.Status, stream.CanRead);
    }

    /// <summary>
    /// Empty input is not an error and never contacts the cluster.
    /// </summary>
    private async Task EmptyBatchIsSkippedAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("--- 5. Empty batch");

        var result = await ingestor.IngestAsync(
            Array.Empty<DeviceReading>(),
            JsonTarget(),
            cancellationToken: cancellationToken);

        logger.LogInformation("Status {Status}, IsSuccess {IsSuccess}", result.Status, result.IsSuccess);
    }

    /// <summary>
    /// The mapping's paths are camelCase; writing PascalCase JSON still "succeeds", but the columns stay empty.
    /// </summary>
    private async Task MappingMismatchLeavesColumnsEmptyAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("--- 6. Mapping mismatch: PascalCase JSON against camelCase mapping paths");

        // Verbatim (PascalCase) names: "DeviceId" never matches the mapping path "$.deviceId".
        // runId and scenario are written in camelCase so the rows can still be found and read back.
        var pascalCase = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var rows = Readings("mismatch", count: 1)
            .Select(r => new
            {
                runId = r.RunId,
                scenario = r.Scenario,
                r.DeviceId,
                r.SerialNumber,
                r.Value,
                r.Timestamp,
            });

        var result = await ingestor.IngestAsync(
            rows,
            JsonTarget() with { Mode = IngestionMode.Streaming },
            pascalCase,
            cancellationToken);

        logger.LogInformation("Status {Status} - no error, but look at the columns:", result.Status);

        if (result.Status == KustoIngestionStatus.Succeeded)
        {
            await LogRowsAsync("mismatch", cancellationToken);
        }
    }

    /// <summary>
    /// Operational failures come back as Status = Failed; EnsureSuccess() turns them into an exception.
    /// </summary>
    private async Task FailuresAreReturnedNotThrownAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("--- 7. A failure is returned, not thrown");

        var result = await ingestor.IngestAsync(
            Readings("failure", count: 1),
            JsonTarget() with { TableName = "TableThatDoesNotExist" },
            cancellationToken: cancellationToken);

        logger.LogInformation("Status {Status}, IsSuccess {IsSuccess}, error: {Error}", result.Status, result.IsSuccess, result.ErrorMessage);

        try
        {
            result.EnsureSuccess();
        }
        catch (KustoIngestionException ex)
        {
            logger.LogInformation(ex, "EnsureSuccess() threw {Exception}: {Message}", ex.GetType().Name, ex.Message);
        }
    }

    /// <summary>
    /// The cluster reads the blob itself, so the URI must be readable by the cluster (e.g. a SAS URL).
    /// </summary>
    private async Task BlobAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("--- 8. Blob (CSV)");

        if (blobUri is null)
        {
            logger.LogInformation("Skipped: set ATC_KUSTO_INGEST_BLOB_URL to a cluster-readable CSV blob (e.g. a SAS URL) to run it");
            return;
        }

        var result = await ingestor.IngestFromBlobAsync(
            blobUri,
            new KustoIngestTarget { TableName = TableName, Format = KustoIngestFormat.Csv, Mode = IngestionMode.Queued },
            cancellationToken);

        logger.LogInformation("Status {Status}, error: {Error}", result.Status, result.ErrorMessage);
    }

    private KustoIngestTarget JsonTarget()
        => new()
        {
            TableName = TableName,
            Format = KustoIngestFormat.MultiJson,
            MappingReference = MappingName,
        };

    private IEnumerable<DeviceReading> Readings(
        string scenario,
        int count)
        => Enumerable
            .Range(1, count)
            .Select(i => new DeviceReading(
                runId,
                scenario,
                DeviceId: $"device-{scenario}-{i}",
                SerialNumber: $"SN-{i:0000}",
                Value: 20 + i,
                Timestamp: DateTimeOffset.UtcNow));

    private async Task LogRowsAsync(
        string scenario,
        CancellationToken cancellationToken)
    {
        var rows = await processor.ExecuteQuery(
            new ReadingsByScenarioQuery(runId, scenario),
            cancellationToken: cancellationToken) ?? [];

        foreach (var row in rows)
        {
            logger.LogInformation(
                "  read back: deviceId={DeviceId} serialNumber={SerialNumber} value={Value} timestamp={Timestamp}",
                row.DeviceId ?? "(empty)",
                row.SerialNumber ?? "(empty)",
                row.Value?.ToString(CultureInfo.InvariantCulture) ?? "(empty)",
                row.Timestamp?.ToString("O", CultureInfo.InvariantCulture) ?? "(empty)");
        }
    }
}