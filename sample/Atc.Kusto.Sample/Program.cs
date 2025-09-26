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

serviceCollection.ConfigureAzureDataExplorer(
    o =>
    {
        o.HostAddress = new Uri("https://help.kusto.windows.net/");
        o.DatabaseName = "Samples";
        o.Credential = new DefaultAzureCredential();
    },
    "Samples");

var serviceProvider = serviceCollection.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var kustoProcessorFactory = serviceProvider.GetRequiredService<IKustoProcessorFactory>();

var contosoSalesKustoProcessor = kustoProcessorFactory.Create("ContosoSales");
var samplesKustoProcessor = kustoProcessorFactory.Create("Samples");

logger.LogInformation("Querying by existing customer");
var customerByIdQueryExisting = new CustomerByIdQuery(145);
var customerByIdQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(customerByIdQueryExisting, cancellationToken: CancellationToken.None);
logger.LogInformation("\tCustomer Name for Id 145: {CustomerName}", customerByIdQueryResult?.FirstOrDefault()?.FirstName ?? "Unknown");

logger.LogInformation("Querying by non-existing customer");
var customerByIdQueryNonExisting = new CustomerByIdQuery(long.MaxValue);
var customerByIdQueryNonExistingResult = await contosoSalesKustoProcessor.ExecuteQuery(customerByIdQueryNonExisting, cancellationToken: CancellationToken.None);
logger.LogInformation(customerByIdQueryNonExistingResult?.Length > 1
    ? "\tIncorrectly found non-existing customer"
    : "\tDid not find non-existing customer as expected");

logger.LogInformation("Querying for customer genders and counts");
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
        logger.LogInformation(ex, "\tQuery was canceled as expected (server received cancel command).");
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
        logger.LogInformation(ex, "\tQuery was canceled (server did NOT receive cancel command, continues running).");
    }
}

logger.LogInformation("Querying storm events");
var stormEventsResult = await samplesKustoProcessor.ExecuteQuery(new StormEventsQuery(), cancellationToken: CancellationToken.None);
if (stormEventsResult is not null)
{
    logger.LogInformation("\tFound {Count} storm events.", stormEventsResult.Length);

    foreach (var stormEvent in stormEventsResult)
    {
        logger.LogInformation(
            "\t{StartTime}\t{EventType,-20}\t{State}",
            stormEvent.StartTime.ToString(CultureInfo.InvariantCulture),
            stormEvent.EventType,
            stormEvent.State);
    }
}

var streamingQuery = new CustomersStreamingQuery();

logger.LogInformation("Streaming customers without streaming query result..");

var countWithoutStreamingQueryResult = 0;
await foreach (var customer in contosoSalesKustoProcessor.ExecuteStreamingQuery(streamingQuery, CancellationToken.None))
{
    logger.LogInformation("\t {FirstName} {LastName}", customer.FirstName, customer.LastName);
    countWithoutStreamingQueryResult++;
}

logger.LogInformation("Streamed {CountWithoutStreamingQueryResult} customers without streaming query result", countWithoutStreamingQueryResult);
logger.LogInformation("Streaming without streaming query result complete.");

logger.LogInformation("Executing streaming query with streaming query result");

var streamingResult = await contosoSalesKustoProcessor.ExecuteBufferedStreamingQuery(
    streamingQuery,
    CancellationToken.None);

if (streamingResult is null)
{
    logger.LogInformation("No streaming result received.");
    return;
}

var countWithStreamingQueryResult = 0;
await foreach (var customer in streamingResult.Rows.WithCancellation(CancellationToken.None))
{
    logger.LogInformation("\t {FirstName} {LastName}", customer.FirstName, customer.LastName);
    countWithStreamingQueryResult++;
}

logger.LogInformation("Streamed {CountWithStreamingQueryResult} customers with frames.", countWithStreamingQueryResult);

logger.LogInformation(streamingResult.Header is not null
    ? $"Header: Version={streamingResult.Header.Version}, Progressive={streamingResult.Header.IsProgressive}"
    : "No header received.");

if (streamingResult.TableSchemas is not null && streamingResult.TableSchemas.Count > 0)
{
    foreach (var schema in streamingResult.TableSchemas)
    {
        logger.LogInformation($"Schema for table '{schema.TableName}':");
        foreach (var col in schema.Columns)
        {
            logger.LogInformation($"\tColumn: {col.Name} (Type: {col.Type})");
        }
    }
}
else
{
    logger.LogInformation("No table schema information received.");
}

logger.LogInformation(streamingResult.Completion is not null
    ? $"Completion: HasErrors={streamingResult.Completion.HasErrors}" +
      (!string.IsNullOrEmpty(streamingResult.Completion.ErrorMessage)
          ? $", Error Message: {streamingResult.Completion.ErrorMessage}"
          : string.Empty)
    : "No completion summary received.");

logger.LogInformation("Streaming with streaming query result complete.");

// Warm up connection by fetching first row to establish connection and authenticate
logger.LogInformation("Warming up Kusto connection...");
var warmupEnumerator = contosoSalesKustoProcessor.ExecuteStreamingQuery(streamingQuery, CancellationToken.None).GetAsyncEnumerator();

try
{
    await warmupEnumerator.MoveNextAsync();
    logger.LogInformation("Connection established.");
}
finally
{
    await warmupEnumerator.DisposeAsync();
}

// Demonstrate cancellation with server-side cancel enabled (default)
logger.LogInformation("Demonstrating cancellation of a long-running streaming query (server-side cancel enabled)...");
var rowCount = 0;
using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
{
    try
    {
        await foreach (var x in contosoSalesKustoProcessor.ExecuteStreamingQuery(streamingQuery, cts.Token))
        {
            if (rowCount == 0)
            {
                logger.LogInformation("Received first row at {Time}", DateTime.Now);
            }

            rowCount++;
        }

        logger.LogInformation("Streaming query completed successfully after receiving {RowCount} rows.", rowCount);
    }
    catch (OperationCanceledException ex)
    {
        logger.LogInformation(ex, "Streaming query was canceled as expected after receiving {RowCount} rows. (server - side cancel ENABLED)", rowCount);
    }
}

// Demonstrate cancellation with server-side cancel disabled
logger.LogInformation("Demonstrating cancellation with server-side cancel disabled...");
var rowCount2 = 0;
using (var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
{
    try
    {
        await foreach (var x in contosoSalesKustoProcessor.ExecuteStreamingQuery(
                           streamingQuery,
                           new AtcStreamingQueryOptions { EnableServerSideCancellation = false },
                           cts2.Token))
        {
            rowCount2++;
        }

        logger.LogInformation("Streaming query completed successfully after receiving {RowCount} rows.", rowCount2);
    }
    catch (OperationCanceledException ex)
    {
        logger.LogInformation(ex, "Streaming query was canceled after receiving {RowCount} rows. (server - side cancel DISABLED)", rowCount2);
    }
}

logger.LogInformation("Press any key to exit");
Console.ReadLine();