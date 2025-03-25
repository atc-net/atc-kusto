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
var customerByIdQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(customerByIdQueryExisting, CancellationToken.None);
logger.LogInformation("\tCustomer Name for Id 145: {CustomerName}", customerByIdQueryResult?.FirstOrDefault()?.FirstName ?? "Unknown");

logger.LogInformation("Querying by non-existing customer");
var customerByIdQueryNonExisting = new CustomerByIdQuery(long.MaxValue);
var customerByIdQueryNonExistingResult = await contosoSalesKustoProcessor.ExecuteQuery(customerByIdQueryNonExisting, CancellationToken.None);
logger.LogInformation(customerByIdQueryNonExistingResult?.Length > 1
    ? "\tIncorrectly found non-existing customer"
    : "\tDid not find non-existing customer as expected");

logger.LogInformation("Querying for customer genders and counts");
var customersSplitByGenderQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomersSplitByGenderQuery(), CancellationToken.None);
foreach (var customerGenderCount in customersSplitByGenderQueryResult!.Counts)
{
    logger.LogInformation("\tFound {Count} {Gender}", customerGenderCount.Count, customerGenderCount.Gender);
}

logger.LogInformation("Querying for customer sales");
var customersSalesQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomerSalesQuery(), CancellationToken.None);
var customerSales = customersSalesQueryResult!.SingleOrDefault(x => x.CustomerKey == 145);
if (customerSales is not null)
{
    logger.LogInformation("\tFound sales amount {SalesAmount} for customer 145.", customerSales.SalesAmount);
}

logger.LogInformation("Querying storm events");
var stormEventsResult = await samplesKustoProcessor.ExecuteQuery(new StormEventsQuery(), CancellationToken.None);
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

logger.LogInformation("Press any key to exit");
Console.ReadLine();