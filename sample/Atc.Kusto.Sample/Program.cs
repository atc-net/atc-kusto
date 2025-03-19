var serviceCollection = new ServiceCollection();

serviceCollection.ConfigureAzureDataExplorer(o =>
{
    o.HostAddress = new Uri("https://help.kusto.windows.net/");
    o.DatabaseName = "ContosoSales";
    o.Credential = new DefaultAzureCredential();
});

var serviceProvider = serviceCollection.BuildServiceProvider();
var kustoProcessor = serviceProvider.GetRequiredService<IKustoProcessor>();

Console.WriteLine("Querying by existing customer");
var customerByIdQueryExisting = new CustomerByIdQuery(145);
var customerByIdQueryResult = await kustoProcessor.ExecuteQuery(customerByIdQueryExisting, CancellationToken.None);
Console.WriteLine($"\tCustomer Name for Id 145: {customerByIdQueryResult?.FirstOrDefault()?.FirstName ?? "Unknown"}");

Console.WriteLine("Querying by non-existing customer");
var customerByIdQueryNonExisting = new CustomerByIdQuery(long.MaxValue);
var customerByIdQueryNonExistingResult = await kustoProcessor.ExecuteQuery(customerByIdQueryNonExisting, CancellationToken.None);
Console.WriteLine(customerByIdQueryNonExistingResult?.Length > 1 ? "\tIncorrectly found non-existing customer" : "\tDid not find non-existing customer as expected");

Console.WriteLine("Querying for customer genders and counts");
var customersSplitByGenderQueryResult = await kustoProcessor.ExecuteQuery(new CustomersSplitByGenderQuery(), CancellationToken.None);
foreach (var customerGenderCount in customersSplitByGenderQueryResult!.Counts)
{
    Console.WriteLine($"\t Found {customerGenderCount.Count} {customerGenderCount.Gender}");
}

Console.WriteLine("Querying for customer sales");
var customersSalesQueryResult = await kustoProcessor.ExecuteQuery(new CustomerSalesQuery(), CancellationToken.None);
var customerSales = customersSalesQueryResult!.SingleOrDefault(x => x.CustomerKey == 145);
if (customerSales is not null)
{
    Console.WriteLine($"\tFound sales amount {customerSales.SalesAmount} for customer 145.");
}

Console.WriteLine("Press any key to exit");
Console.ReadLine();