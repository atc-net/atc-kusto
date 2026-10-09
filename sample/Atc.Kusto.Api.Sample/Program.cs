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
        async static Task<Results<Ok<PagedResult<Customer>>, ProblemHttpResult>> (
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
                        cancellationToken)
                switch
                {
                    { } page => TypedResults.Ok(page),

                    // null today means an invalid/expired continuation token, or a failed query (logged).
                    _ when continuationToken is not null => TypedResults.Problem(
                        "The continuation token is invalid or has expired. Start again without a token.",
                        statusCode: StatusCodes.Status400BadRequest),
                    _ => QueryFailed(),
                })
    .WithName("GetCustomers")
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .WithDescription("Get all customers")
    .WithOpenApi();

app.MapGet(
        "/customers/{customerId}",
        async static Task<Results<Ok<Customer>, NotFound>> (
            long customerId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => await processorFactory.Create("ContosoSales")
                    .ExecuteQuery(
                        new CustomersQuery(customerId),
                        cancellationToken: cancellationToken)
                switch
                {
                    [{ } customer] => TypedResults.Ok(customer),
                    _ => TypedResults.NotFound(),
                })
    .WithName("GetCustomerById")
    .WithDescription("Get customer by id")
    .WithOpenApi();

app.MapGet(
        "/customers/sales",
        async static Task<Results<Ok<CustomerSales[]>, ProblemHttpResult>> (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => await processorFactory.Create("ContosoSales")
                    .ExecuteQuery(
                        new CustomerSalesQuery(),
                        new AtcQueryOptions
                        {
                            QueryTakeMaxRecords = 10,
                            TruncationMaxRecords = 20,
                        },
                        cancellationToken: cancellationToken)
                is { } sales
                    ? TypedResults.Ok(sales)
                    : QueryFailed())
    .WithName("GetCustomerSales")
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .WithDescription("Get summarized sales amounts per customer")
    .WithOpenApi();

app.MapGet(
        "/customers/stream-with-streaming-query-result",
        async static Task<Results<Ok<StreamingQueryResult<Customer>>, ProblemHttpResult>> (
            [FromHeader(Name = "x-client-session-id")] string? sessionId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => await processorFactory.Create("ContosoSales")
                    .ExecuteBufferedStreamingQuery(
                        new CustomersStreamingQuery(),
                        cancellationToken)
                is { } streamingQueryResult
                    ? TypedResults.Ok(streamingQueryResult)
                    : QueryFailed())
    .WithName("GetCustomersStreamWithStreamingQueryResult")
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .WithDescription("Streaming customers with streaming query result")
    .WithOpenApi();

app.MapGet(
        "/customers/stream",
        static (
            [FromHeader(Name = "x-client-session-id")] string? sessionId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => TypedResults.Ok(processorFactory.Create("ContosoSales")
                .ExecuteStreamingQuery(
                    new CustomersStreamingQuery(),
                    cancellationToken)))
    .WithName("GetCustomersStream")
    .WithDescription("Streaming customers")
    .WithOpenApi();

app.MapGet(
        "/customers/stream-cancel-demo",
        async Task<Results<Ok, StatusCodeHttpResult>> (
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

                return TypedResults.Ok();
            }
            catch (OperationCanceledException)
            {
                return TypedResults.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
        })
    .WithName("GetCustomersStreamCancelDemo")
    .Produces(StatusCodes.Status499ClientClosedRequest)
    .WithDescription("Demonstrates cancellation of streaming with server-side cancel enabled")
    .WithOpenApi();

app.MapGet(
        "/customers/stream-cancel-demo-no-server",
        async Task<Results<Ok, StatusCodeHttpResult>> (
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

                return TypedResults.Ok();
            }
            catch (OperationCanceledException)
            {
                return TypedResults.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
        })
    .WithName("GetCustomersStreamCancelDemoNoServer")
    .Produces(StatusCodes.Status499ClientClosedRequest)
    .WithDescription("Demonstrates cancellation of streaming with server-side cancel disabled")
    .WithOpenApi();

app.MapGet(
        "/nyctaxitrips/stream-with-streaming-query-result",
        async static Task<Results<Ok<StreamingQueryResult<NycTaxiTrip>>, ProblemHttpResult>> (
            [FromHeader(Name = "x-client-session-id")] string? sessionId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => await processorFactory.Create("Samples")
                    .ExecuteBufferedStreamingQuery(
                        new NycTaxiTripsStreamingQuery(),
                        new AtcStreamingQueryOptions
                        {
                            NoTruncation = true,
                        },
                        cancellationToken)
                is { } streamingQueryResult
                    ? TypedResults.Ok(streamingQueryResult)
                    : QueryFailed())
    .WithName("GetNycTaxiTripsStreamWithStreamingQueryResult")
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .WithDescription("Streaming nyc taxi trips with streaming query result")
    .WithOpenApi();

app.MapGet(
        "/nyctaxitrips/stream",
        static (
            [FromHeader(Name = "x-client-session-id")] string? sessionId,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => TypedResults.Ok(processorFactory.Create("Samples")
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
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
    .WithDescription("Ingest device readings (200 = in the table, 202 = queued, 502 = failed). Optional ?mode=Streaming|ManagedStreaming|Queued")
    .WithOpenApi();

await app.RunAsync();

// Atc.Kusto query methods return null when the query failed (the error is logged by the library).
static ProblemHttpResult QueryFailed()
    => TypedResults.Problem(
        "The Kusto query failed; see the application logs.",
        statusCode: StatusCodes.Status502BadGateway,
        title: "Query failed");