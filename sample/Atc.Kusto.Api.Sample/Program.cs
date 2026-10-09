var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureAzureDataExplorer(
    o =>
    {
        o.HostAddress = new Uri("https://help.kusto.windows.net/");
        o.DatabaseName = "ContosoSales";
        o.Credential = new DefaultAzureCredential();
    },
    "ContosoSales");

builder.Services.ConfigureAzureDataExplorer(
    o =>
    {
        o.HostAddress = new Uri("https://help.kusto.windows.net/");
        o.DatabaseName = "Samples";
        o.Credential = new DefaultAzureCredential();
    },
    "Samples");

// Optional writable connection for POST /device-readings (help.kusto.windows.net is read-only).
// Run sample/Atc.Kusto.Ingestion.Sample/setup.kql against the database first.
var ingestionHostAddress = builder.Configuration["Ingestion:HostAddress"];
var ingestionDatabaseName = builder.Configuration["Ingestion:DatabaseName"];
var isIngestionConfigured = !string.IsNullOrWhiteSpace(ingestionHostAddress) &&
                            !string.IsNullOrWhiteSpace(ingestionDatabaseName);

if (isIngestionConfigured)
{
    builder.Services.ConfigureAzureDataExplorer(
        new Uri(ingestionHostAddress!),
        ingestionDatabaseName!,
        new DefaultAzureCredential(),
        "Ingestion");
}

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.ConfigureSwagger();

app.UseHttpsRedirection();

app.MapGet(
        "/customers",
        async static (
                [FromHeader(Name = "x-client-session-id")] string? sessionId,
                [FromHeader(Name = "x-pageSize")] int? pageSize,
                [FromHeader(Name = "x-continuation-token")] string? continuationToken,
                IKustoProcessorFactory processorFactory,
                CancellationToken cancellationToken)
            => await processorFactory
                .Create("ContosoSales")
                .ExecutePagedQuery(
                    new CustomersQuery(),
                    sessionId,
                    pageSize ?? 100,
                    continuationToken,
                    cancellationToken))
    .WithName("GetCustomers")
    .WithDescription("Get all customers")
    .WithOpenApi();

app.MapGet(
        "/customers/{customerId}",
        async static (
            long customerId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => (IResult)(await processorFactory.Create("ContosoSales")
                    .ExecuteQuery(
                        new CustomersQuery(customerId),
                        cancellationToken: cancellationToken)
                switch
                {
                    [{ } customer] => TypedResults.Ok((object?)customer),
                    _ => TypedResults.NotFound(),
                }))
    .WithName("GetCustomerById")
    .WithDescription("Get customer by id")
    .WithOpenApi();

app.MapGet(
        "/customers/sales",
        (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => processorFactory.Create("ContosoSales")
                .ExecuteQuery(
                    new CustomerSalesQuery(),
                    new AtcQueryOptions
                    {
                        QueryTakeMaxRecords = 10,
                        TruncationMaxRecords = 20,
                    },
                    cancellationToken: cancellationToken))
    .WithName("GetCustomerSales")
    .WithDescription("Get summarized sales amounts per customer")
    .WithOpenApi();

app.MapGet(
        "/customers/stream-with-streaming-query-result",
        async static (
            [FromHeader(Name = "x-client-session-id")] string? sessionId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            =>
            {
                var streamingQueryResult = await processorFactory.Create("ContosoSales")
                    .ExecuteBufferedStreamingQuery(
                        new CustomersStreamingQuery(),
                        cancellationToken);

                return TypedResults.Ok(streamingQueryResult);
            })
    .WithName("GetCustomersStreamWithStreamingQueryResult")
    .WithDescription("Streaming customers with streaming query result")
    .WithOpenApi();

app.MapGet(
        "/customers/stream",
        (
            [FromHeader(Name = "x-client-session-id")] string? sessionId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => Task.FromResult(processorFactory.Create("ContosoSales")
                .ExecuteStreamingQuery(
                    new CustomersStreamingQuery(),
                    cancellationToken)))
    .WithName("GetCustomersStream")
    .WithDescription("Streaming customers")
    .WithOpenApi();

app.MapGet(
        "/customers/stream-cancel-demo",
        async (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken) =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));

            try
            {
                await foreach (var x in processorFactory.Create("ContosoSales")
                                   .ExecuteStreamingQuery(new CustomersStreamingQuery(), cts.Token))
                {
                    // no-op
                }

                return Results.Ok();
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
        })
    .WithName("GetCustomersStreamCancelDemo")
    .WithDescription("Demonstrates cancellation of streaming with server-side cancel enabled")
    .WithOpenApi();

app.MapGet(
        "/customers/stream-cancel-demo-no-server",
        async (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken) =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));

            try
            {
                await foreach (var x in processorFactory.Create("ContosoSales")
                                   .ExecuteStreamingQuery(new CustomersStreamingQuery(), new AtcStreamingQueryOptions { EnableServerSideCancellation = false }, cts.Token))
                {
                    // no-op
                }

                return Results.Ok();
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
        })
    .WithName("GetCustomersStreamCancelDemoNoServer")
    .WithDescription("Demonstrates cancellation of streaming with server-side cancel disabled")
    .WithOpenApi();

app.MapGet(
        "/nyctaxitrips/stream-with-streaming-query-result",
        async static (
            [FromHeader(Name = "x-client-session-id")] string? sessionId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            =>
            {
                var streamingQueryResult = await processorFactory.Create("Samples")
                    .ExecuteBufferedStreamingQuery(
                        new NycTaxiTripsStreamingQuery(),
                        new AtcStreamingQueryOptions
                        {
                            NoTruncation = true,
                        },
                        cancellationToken);

                return TypedResults.Ok(streamingQueryResult);
            })
    .WithName("GetNycTaxiTripsStreamWithStreamingQueryResult")
    .WithDescription("Streaming nyc taxi trips with streaming query result")
    .WithOpenApi();

app.MapGet(
        "/nyctaxitrips/stream",
        (
            [FromHeader(Name = "x-client-session-id")] string? sessionId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => Task.FromResult(processorFactory.Create("Samples")
                .ExecuteStreamingQuery(
                    new NycTaxiTripsStreamingQuery(),
                    new AtcStreamingQueryOptions
                    {
                        NoTruncation = true,
                    },
                    cancellationToken)))
    .WithName("GetNycTaxiTripsStream")
    .WithDescription("Streaming nyc taxi trips")
    .WithOpenApi();

app.MapPost(
        "/device-readings",
        async Task<Results<Ok<KustoIngestionResult>, Accepted<KustoIngestionResult>, ProblemHttpResult>> (
            DeviceReadingRequest[] readings,
            IngestionMode? mode,
            IKustoIngestor ingestor,
            CancellationToken cancellationToken) =>
        {
            if (!isIngestionConfigured)
            {
                return TypedResults.Problem(
                    "Set Ingestion:HostAddress and Ingestion:DatabaseName to a writable cluster and run setup.kql from Atc.Kusto.Ingestion.Sample.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Ingestion is not configured");
            }

            var runId = Guid.NewGuid().ToString("N");
            var rows = readings.Select(r => new IngestedDeviceReading(
                runId,
                Scenario: "api",
                r.DeviceId,
                r.SerialNumber,
                r.Value,
                r.Timestamp));

            var result = await ingestor.IngestAsync(
                rows,
                new KustoIngestTarget
                {
                    TableName = "SampleDeviceReadings",
                    Format = KustoIngestFormat.MultiJson,
                    MappingReference = "SampleDeviceReadings_mapping",
                    ConnectionName = "Ingestion",
                    Mode = mode,
                },
                cancellationToken: cancellationToken);

            // Ingestion failures are returned, not thrown, so map every status explicitly.
            return result.Status switch
            {
                KustoIngestionStatus.Succeeded or KustoIngestionStatus.Skipped => TypedResults.Ok(result),
                KustoIngestionStatus.Queued => TypedResults.Accepted(uri: (string?)null, value: result),
                _ => TypedResults.Problem(
                    result.ErrorMessage,
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Ingestion failed"),
            };
        })
    .WithName("IngestDeviceReadings")
    .WithDescription("Ingest device readings (200 = in the table, 202 = queued, 502 = failed). Optional ?mode=Streaming|ManagedStreaming|Queued")
    .WithOpenApi();

await app.RunAsync();