var serviceCollection = new ServiceCollection();

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
var kustoProcessorFactory = serviceProvider.GetRequiredService<IKustoProcessorFactory>();

var contosoSalesKustoProcessor = kustoProcessorFactory.Create("ContosoSales");
var samplesKustoProcessor = kustoProcessorFactory.Create("Samples");

Console.WriteLine("Querying by existing customer");
var customerByIdQueryExisting = new CustomerByIdQuery(145);
var customerByIdQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(customerByIdQueryExisting, CancellationToken.None);
Console.WriteLine($"\tCustomer Name for Id 145: {customerByIdQueryResult?.FirstOrDefault()?.FirstName ?? "Unknown"}");

Console.WriteLine("Querying by non-existing customer");
var customerByIdQueryNonExisting = new CustomerByIdQuery(long.MaxValue);
var customerByIdQueryNonExistingResult = await contosoSalesKustoProcessor.ExecuteQuery(customerByIdQueryNonExisting, CancellationToken.None);
Console.WriteLine(customerByIdQueryNonExistingResult?.Length > 1 ? "\tIncorrectly found non-existing customer" : "\tDid not find non-existing customer as expected");

Console.WriteLine("Querying for customer genders and counts");
var customersSplitByGenderQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomersSplitByGenderQuery(), CancellationToken.None);
foreach (var customerGenderCount in customersSplitByGenderQueryResult!.Counts)
{
    Console.WriteLine($"\tFound {customerGenderCount.Count} {customerGenderCount.Gender}");
}

Console.WriteLine("Querying for customer sales");
var customersSalesQueryResult = await contosoSalesKustoProcessor.ExecuteQuery(new CustomerSalesQuery(), CancellationToken.None);
var customerSales = customersSalesQueryResult!.SingleOrDefault(x => x.CustomerKey == 145);
if (customerSales is not null)
{
    Console.WriteLine($"\tFound sales amount {customerSales.SalesAmount} for customer 145.");
}

Console.WriteLine("Querying storm events");
var stormEventsResult = await samplesKustoProcessor.ExecuteQuery(new StormEventsQuery(), CancellationToken.None);
if (stormEventsResult is not null)
{
    Console.WriteLine($"\tFound {stormEventsResult.Length} storm events.");

    foreach (var stormEvent in stormEventsResult)
    {
        Console.WriteLine($"\t{stormEvent.StartTime.ToString(CultureInfo.InvariantCulture)}\t{stormEvent.EventType,-20}\t{stormEvent.State}");
    }
}

Console.WriteLine("Press any key to exit");
Console.ReadLine();