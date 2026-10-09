// Atc.Kusto ingestion sample. Needs a WRITABLE cluster (help.kusto.windows.net is read-only).
// See README.md in this folder: run setup.kql once, then set the environment variables below.
var clusterUrl = Environment.GetEnvironmentVariable("ATC_KUSTO_INGEST_CLUSTER");
var databaseName = Environment.GetEnvironmentVariable("ATC_KUSTO_INGEST_DATABASE");
var blobUrl = Environment.GetEnvironmentVariable("ATC_KUSTO_INGEST_BLOB_URL");

if (string.IsNullOrWhiteSpace(clusterUrl) ||
    string.IsNullOrWhiteSpace(databaseName))
{
    Console.WriteLine("Not configured, nothing ingested.");
    Console.WriteLine("1. Run setup.kql against a writable database (e.g. a free cluster: https://aka.ms/kustofree).");
    Console.WriteLine("2. Set ATC_KUSTO_INGEST_CLUSTER (e.g. https://mycluster.westeurope.kusto.windows.net) and ATC_KUSTO_INGEST_DATABASE.");
    Console.WriteLine("3. Sign in so DefaultAzureCredential finds you (e.g. 'az login'), then run the sample again.");
    return;
}

var services = new ServiceCollection();

services.AddLogging(builder => builder.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
}));

services.ConfigureAzureDataExplorer(
    new Uri(clusterUrl),
    databaseName,
    new DefaultAzureCredential());

await using var serviceProvider = services.BuildServiceProvider();

var scenarios = new IngestionScenarios(
    serviceProvider.GetRequiredService<IKustoIngestor>(),
    serviceProvider.GetRequiredService<IKustoProcessor>(),
    serviceProvider.GetRequiredService<ILogger<IngestionScenarios>>(),
    runId: Guid.NewGuid().ToString("N"),
    blobUri: string.IsNullOrWhiteSpace(blobUrl) ? null : new Uri(blobUrl));

await scenarios.RunAllAsync(CancellationToken.None);