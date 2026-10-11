var serviceCollection = new ServiceCollection();

serviceCollection.AddLogging(builder =>
{
    builder.AddConsole(options =>
    {
        options.FormatterName = "singleline";
    });

    builder.AddConsoleFormatter<SingleLineConsoleFormatter, ConsoleFormatterOptions>();
});

serviceCollection.ConfigureAzureDataExplorer(
    o =>
    {
        o.HostAddress = new Uri("https://help.kusto.windows.net/");
        o.DatabaseName = "ContosoSales";
        o.Credential = new DefaultAzureCredential();
    },
    "ContosoSales");

var serviceProvider = serviceCollection.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var kustoProcessorFactory = serviceProvider.GetRequiredService<IKustoProcessorFactory>();

var contosoSalesKustoProcessor = kustoProcessorFactory.Create("ContosoSales");

// Another database on the same cluster: the same connection, with the database chosen per processor.
var samplesKustoProcessor = kustoProcessorFactory.Create("ContosoSales", databaseName: "Samples");

logger.LogInformation("Querying by existing customer");
var customerByIdQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomerByIdQuery(145), cancellationToken: CancellationToken.None);
logger.LogInformation("\tCustomer Name for Id 145: {CustomerName}", customerByIdQueryResult?.FirstOrDefault()?.FirstName ?? "Unknown");

logger.LogInformation("Querying by non-existing customer");
var customerByIdQueryNonExistingResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomerByIdQuery(long.MaxValue), cancellationToken: CancellationToken.None);
logger.LogInformation(customerByIdQueryNonExistingResult?.Length > 0
    ? "\tIncorrectly found non-existing customer"
    : "\tDid not find non-existing customer as expected");

logger.LogInformation("Querying for customer genders and counts (a query with several result sets)");
var customersSplitByGenderQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomersSplitByGenderQuery(), cancellationToken: CancellationToken.None);
foreach (var customerGenderCount in customersSplitByGenderQueryResult!.Counts)
{
    logger.LogInformation("\tFound {Count} {Gender}", customerGenderCount.Count, customerGenderCount.Gender);
}

logger.LogInformation("Querying for customer sales");
var customersSalesQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomerSalesQuery(), cancellationToken: CancellationToken.None);
var customerSales = customersSalesQueryResult!.SingleOrDefault(x => x.CustomerKey == 145);
if (customerSales is not null)
{
    logger.LogInformation("\tFound sales amount {SalesAmount} for customer 145.", customerSales.SalesAmount);
}

// Demonstrate cancellation on regular queries (non-streaming)
logger.LogInformation("Demonstrating query cancellation with server-side cancel enabled (default)...");
using (var queryCts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
{
    try
    {
        var cancelledQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomerSalesQuery(), cancellationToken: queryCts1.Token);
        logger.LogInformation("\tQuery completed successfully with {Count} results.", cancelledQueryResult?.Length ?? 0);
    }
    catch (OperationCanceledException ex)
    {
        logger.LogInformation(ex, "\tQuery was canceled as expected (the query was also cancelled on the cluster).");
    }
}

logger.LogInformation("Demonstrating query cancellation with server-side cancel disabled...");
using (var queryCts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
{
    try
    {
        var cancelledQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(
            new CustomerSalesQuery(),
            new AtcQueryOptions { EnableServerSideCancellation = false },
            cancellationToken: queryCts2.Token);
        logger.LogInformation("\tQuery completed successfully with {Count} results.", cancelledQueryResult?.Length ?? 0);
    }
    catch (OperationCanceledException ex)
    {
        logger.LogInformation(ex, "\tQuery was canceled (the query keeps running on the cluster until it completes).");
    }
}

logger.LogInformation("Querying storm events (in the Samples database)");
var stormEventsResult = await samplesKustoProcessor.ExecuteQuery(new StormEventsQuery(), cancellationToken: CancellationToken.None);
logger.LogInformation("\tFound {Count} storm events.", stormEventsResult!.Length);
foreach (var stormEvent in stormEventsResult)
{
    logger.LogInformation(
        "\t{StartTime}\t{EventType,-20}\t{State}",
        stormEvent.StartTime.ToString(CultureInfo.InvariantCulture),
        stormEvent.EventType,
        stormEvent.State);
}

var streamingQuery = new CustomersStreamingQuery();

logger.LogInformation("Direct streaming of customers...");
var directCount = 0;
await foreach (var customer in contosoSalesKustoProcessor.ExecuteStreamingQuery(streamingQuery, CancellationToken.None))
{
    if (directCount < 3)
    {
        logger.LogInformation("\t{FirstName} {LastName}", customer.FirstName, customer.LastName);
    }

    directCount++;
}

logger.LogInformation("\tStreamed {Count} customers.", directCount);

logger.LogInformation("Buffered streaming of customers (with header, schemas and completion)...");
var streamingResult = await contosoSalesKustoProcessor.ExecuteBufferedStreamingQuery(
    streamingQuery,
    CancellationToken.None);

if (streamingResult?.Completion?.HasErrors == true)
{
    logger.LogWarning("\tThe query failed: {ErrorMessage}", streamingResult.Completion.ErrorMessage);
}
else if (streamingResult is not null)
{
    var bufferedCount = 0;
    await foreach (var customer in streamingResult.Rows.WithCancellation(CancellationToken.None))
    {
        bufferedCount++;
    }

    logger.LogInformation(
        "\tRead {Count} customers. Header: Version={Version}, Progressive={IsProgressive}",
        bufferedCount,
        streamingResult.Header?.Version,
        streamingResult.Header?.IsProgressive);

    foreach (var schema in streamingResult.TableSchemas ?? [])
    {
        logger.LogInformation(
            "\tSchema for table {TableName}: {Columns}",
            schema.TableName,
            string.Join(", ", schema.Columns.Select(c => $"{c.Name} ({c.Type})")));
    }
}

logger.LogInformation("Streaming customer names, mapped by the query's own MapRecord...");
var nameCount = 0;
await foreach (var name in contosoSalesKustoProcessor.ExecuteStreamingQuery(new CustomerNamesStreamingQuery(), CancellationToken.None))
{
    if (nameCount < 3)
    {
        logger.LogInformation("\t{Name}", name);
    }

    nameCount++;
}

logger.LogInformation("\tStreamed {Count} names.", nameCount);

// Kusto decimals arrive as SqlDecimal and are mapped to decimal properties.
logger.LogInformation("Direct streaming of customer sales (decimal values)...");
var salesStreamingQuery = new CustomerSalesStreamingQuery();
var salesCount = 0;
await foreach (var sale in contosoSalesKustoProcessor.ExecuteStreamingQuery(salesStreamingQuery, CancellationToken.None))
{
    if (salesCount < 3)
    {
        logger.LogInformation(
            "\tCustomer {CustomerName}: SalesAmount={SalesAmount:C}, TotalCost={TotalCost:C}",
            sale.CustomerName,
            sale.SalesAmount,
            sale.TotalCost);
    }

    salesCount++;
}

logger.LogInformation("\tStreamed {SalesCount} customer sales.", salesCount);

// Progressive mode: Kusto may send a preview of a summarize and replace it later. Buffered streaming
// applies the replace before handing out rows, so it's the safe way to use progressive mode.
logger.LogInformation("Buffered streaming of customer sales with progressive mode...");
var progressiveResult = await contosoSalesKustoProcessor.ExecuteBufferedStreamingQuery(
    salesStreamingQuery,
    new AtcStreamingQueryOptions { ProgressiveEnabled = true },
    CancellationToken.None);

if (progressiveResult is not null)
{
    var progressiveCount = 0;
    await foreach (var sale in progressiveResult.Rows.WithCancellation(CancellationToken.None))
    {
        progressiveCount++;
    }

    logger.LogInformation(
        "\tRead {Count} customer sales. Progressive={IsProgressive}, table completions={TableCompletions}, HasErrors={HasErrors}",
        progressiveCount,
        progressiveResult.Header?.IsProgressive,
        progressiveResult.Completion?.TableCompletions?.Count,
        progressiveResult.Completion?.HasErrors);
}

// Demonstrate cancellation with server-side cancel enabled (default)
logger.LogInformation("Demonstrating cancellation of a streaming query (server-side cancel enabled)...");
var rowCount = 0;
using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
{
    try
    {
        await foreach (var customer in contosoSalesKustoProcessor.ExecuteStreamingQuery(streamingQuery, cts.Token))
        {
            rowCount++;
        }

        logger.LogInformation("\tStreaming query completed after {RowCount} rows, before it was cancelled.", rowCount);
    }
    catch (OperationCanceledException ex)
    {
        logger.LogInformation(ex, "\tStreaming query was canceled after {RowCount} rows; it was also cancelled on the cluster.", rowCount);
    }
}

// Demonstrate cancellation with server-side cancel disabled
logger.LogInformation("Demonstrating cancellation of a streaming query (server-side cancel disabled)...");
var rowCount2 = 0;
using (var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
{
    try
    {
        await foreach (var customer in contosoSalesKustoProcessor.ExecuteStreamingQuery(
                           streamingQuery,
                           new AtcStreamingQueryOptions { EnableServerSideCancellation = false },
                           cts2.Token))
        {
            rowCount2++;
        }

        logger.LogInformation("\tStreaming query completed after {RowCount} rows, before it was cancelled.", rowCount2);
    }
    catch (OperationCanceledException ex)
    {
        logger.LogInformation(ex, "\tStreaming query was canceled after {RowCount} rows; it keeps running on the cluster.", rowCount2);
    }
}

logger.LogInformation("Press Enter to exit");
Console.ReadLine();