var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureAzureDataExplorer(
    o =>
    {
        o.HostAddress = new Uri("https://help.kusto.windows.net/");
        o.DatabaseName = "ContosoSales";
        o.Credential = new DefaultAzureCredential();

        // How long the cluster keeps the stored result behind a paged query (default 1 hour).
        o.PagedResultExpiration = TimeSpan.FromMinutes(30);
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

// A failing query throws, for example a KustoServiceException when the cluster can't be reached.
// The exception handler turns that into a 500 problem details response.
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

app.ConfigureSwagger();

app.UseHttpsRedirection();

app.MapGet(
        "/customers",
        async static Task<Results<Ok<PagedResult<Customer>>, ProblemHttpResult>> (
            [FromHeader(Name = "x-pageSize")] int? pageSize,
            [FromHeader(Name = "x-continuation-token")] string? continuationToken,
            bool? includeTotalCount,
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken) =>
        {
            if (pageSize < 1)
            {
                return TypedResults.Problem(
                    "x-pageSize must be at least 1.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // The session id groups the stored results of one user. Take it from the signed-in user
            // (e.g. their object id), never from a header that the client can set. This sample has no
            // sign-in, so it passes null: every first page then gets a stored result of its own.
            var page = await processorFactory
                .Create("ContosoSales")
                .ExecutePagedQuery(
                    new CustomersQuery(),
                    sessionId: null,
                    pageSize ?? 100,
                    continuationToken,
                    new AtcPagedQueryOptions
                    {
                        IncludeTotalCount = includeTotalCount == true,
                    },
                    cancellationToken);

            // null means that the continuation token can't be read, e.g. a token from before Atc.Kusto 4.0.
            return page is not null
                ? TypedResults.Ok(page)
                : TypedResults.Problem(
                    "The continuation token is not valid. Start again without a token.",
                    statusCode: StatusCodes.Status400BadRequest);
        })
    .WithName("GetCustomers")
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .WithDescription("Get all customers, one page at a time. Optional ?includeTotalCount=true")
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
        async static Task<Ok<CustomerSales[]>> (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => TypedResults.Ok(
                await processorFactory.Create("ContosoSales")
                    .ExecuteQuery(
                        new CustomerSalesQuery(),
                        new AtcQueryOptions
                        {
                            QueryTakeMaxRecords = 10,
                            TruncationMaxRecords = 20,
                        },
                        cancellationToken: cancellationToken)
                ?? []))
    .WithName("GetCustomerSales")
    .WithDescription("Get summarized sales amounts per customer (at most 10)")
    .WithOpenApi();

app.MapGet(
        "/customers/stream-with-streaming-query-result",
        async static Task<Results<Ok<StreamingQueryResult<Customer>>, ProblemHttpResult>> (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => BufferedResult(
                await processorFactory.Create("ContosoSales")
                    .ExecuteBufferedStreamingQuery(
                        new CustomersStreamingQuery(),
                        cancellationToken)))
    .WithName("GetCustomersStreamWithStreamingQueryResult")
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .WithDescription("Streaming customers with streaming query result")
    .WithOpenApi();

app.MapGet(
        "/customers/stream",
        static (
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
        async static Task<Ok<string>> (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => TypedResults.Ok(
                await StreamUntilCancelled(
                    processorFactory.Create("ContosoSales"),
                    new AtcStreamingQueryOptions(),
                    cancellationToken)))
    .WithName("GetCustomersStreamCancelDemo")
    .WithDescription("Cancels a streaming query after 50 ms, like a client that disconnects; the query is also cancelled on the cluster")
    .WithOpenApi();

app.MapGet(
        "/customers/stream-cancel-demo-no-server",
        async static Task<Ok<string>> (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => TypedResults.Ok(
                await StreamUntilCancelled(
                    processorFactory.Create("ContosoSales"),
                    new AtcStreamingQueryOptions
                    {
                        EnableServerSideCancellation = false,
                    },
                    cancellationToken)))
    .WithName("GetCustomersStreamCancelDemoNoServer")
    .WithDescription("Cancels a streaming query after 50 ms, like a client that disconnects, without cancelling it on the cluster")
    .WithOpenApi();

app.MapGet(
        "/nyctaxitrips/stream-with-streaming-query-result",
        async static Task<Results<Ok<StreamingQueryResult<NycTaxiTrip>>, ProblemHttpResult>> (
            IKustoProcessorFactory processorFactory,
            CancellationToken cancellationToken)
            => BufferedResult(
                await processorFactory.Create("Samples")
                    .ExecuteBufferedStreamingQuery(
                        new NycTaxiTripsStreamingQuery(),
                        new AtcStreamingQueryOptions
                        {
                            NoTruncation = true,
                        },
                        cancellationToken)))
    .WithName("GetNycTaxiTripsStreamWithStreamingQueryResult")
    .ProducesProblem(StatusCodes.Status502BadGateway)
    .WithDescription("Streaming nyc taxi trips with streaming query result")
    .WithOpenApi();

app.MapGet(
        "/nyctaxitrips/stream",
        static (
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

// A buffered streaming query doesn't throw when the query fails: it reports the error in Completion.
static Results<Ok<StreamingQueryResult<T>>, ProblemHttpResult> BufferedResult<T>(
    StreamingQueryResult<T>? result)
    => result switch
    {
        null => TypedResults.Problem(
            "The Kusto query returned no result.",
            statusCode: StatusCodes.Status502BadGateway,
            title: "Query failed"),
        { Completion.HasErrors: true } => TypedResults.Problem(
            result.Completion.ErrorMessage,
            statusCode: StatusCodes.Status502BadGateway,
            title: "Query failed"),
        _ => TypedResults.Ok(result),
    };

// Cancels the query after 50 ms, like a client that disconnects, and tells how far it got.
static async Task<string> StreamUntilCancelled(
    IKustoProcessor processor,
    AtcStreamingQueryOptions options,
    CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromMilliseconds(50));

    var rows = 0;

    try
    {
        await foreach (var customer in processor.ExecuteStreamingQuery(
                           new CustomersStreamingQuery(),
                           options,
                           cts.Token))
        {
            rows++;
        }

        return $"The query completed before it was cancelled ({rows} rows).";
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return options.EnableServerSideCancellation
            ? $"Cancelled after {rows} rows. The query was also cancelled on the cluster."
            : $"Cancelled after {rows} rows. The query keeps running on the cluster until it completes.";
    }
}