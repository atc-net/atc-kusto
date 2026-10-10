# Introduction

Atc.Kusto is a .NET library designed to facilitate the execution of Kusto queries and commands within Azure Data Explorer environments/clusters.

The library provides a streamlined interface for handling Kusto operations, making it easier to retrieve and process data efficiently.

## Table of Content

- [Introduction](#introduction)
  - [Table of Content](#table-of-content)
  - [Features](#features)
  - [CLI Tool](#cli-tool)
    - [Installation](#installation)
    - [Quick Start](#quick-start)
    - [Authentication](#authentication)
    - [Configuration](#configuration)
    - [Cluster Management](#cluster-management)
    - [Connection Options](#connection-options)
    - [Query Command](#query-command)
      - [Query Options](#query-options)
      - [Output Formats](#output-formats)
      - [Query Validation](#query-validation)
      - [Query Statistics](#query-statistics)
      - [Web Explorer Link](#web-explorer-link)
    - [Database Commands](#database-commands)
    - [Table Commands](#table-commands)
    - [Export Commands](#export-commands)
  - [Getting started](#getting-started)
    - [Configuring the Atc.Kusto library using ServiceCollection Extensions](#configuring-the-atckusto-library-using-servicecollection-extensions)
      - [Setup with Explicit Parameters](#setup-with-explicit-parameters)
      - [Setup with Pre-Configured Options](#setup-with-pre-configured-options)
      - [Setup with Configuration Delegate](#setup-with-configuration-delegate)
    - [Adding a Kusto query](#adding-a-kusto-query)
      - [Required Project Configuration](#required-project-configuration)
      - [Defining Query Parameters](#defining-query-parameters)
    - [Kusto query examples](#kusto-query-examples)
      - [Single](#single)
      - [List](#list)
      - [Complex with multiple result sets](#complex-with-multiple-result-sets)
    - [Executing a Kusto query](#executing-a-kusto-query)
      - [Choosing the connection and database per call](#choosing-the-connection-and-database-per-call)
    - [Executing streaming queries](#executing-streaming-queries)
      - [Direct streaming](#direct-streaming)
      - [Buffered streaming](#buffered-streaming)
    - [Ingesting data](#ingesting-data)
      - [Ingestion quick start](#ingestion-quick-start)
      - [Checking the result](#checking-the-result)
      - [Ingestion modes](#ingestion-modes)
      - [JSON property names and ingestion mappings](#json-property-names-and-ingestion-mappings)
      - [Ingesting a stream or a blob](#ingesting-a-stream-or-a-blob)
      - [KustoIngestTarget](#kustoingesttarget)
      - [Tracking queued ingestion](#tracking-queued-ingestion)
      - [Retries and duplicates](#retries-and-duplicates)
      - [Private endpoints: upload containers](#private-endpoints-upload-containers)
      - [Dependencies](#dependencies)
    - [Health Checks](#health-checks)
      - [Setup Health Check](#setup-health-check)
      - [Health Check Response](#health-check-response)
      - [Health Check Statuses](#health-check-statuses)
      - [Using Health Check Programmatically](#using-health-check-programmatically)
  - [Sample](#sample)
  - [Retries](#retries)
  - [Cancellation](#cancellation)
    - [What happens when you cancel](#what-happens-when-you-cancel)
    - [Configuration](#configuration)
    - [Performance Implications](#performance-implications)
    - [Examples](#examples)
  - [Query Options](#query-options)
    - [Query Timeout](#query-timeout)
  - [Telemetry](#telemetry)
  - [Analyzer](#analyzer)
    - [Analyzer Rules](#analyzer-rules)
  - [Requirements](#requirements)
  - [How to contribute](#how-to-contribute)

## Features

The library extends the official .NET SDK, and adds the following add-on functionality, which supports passing parameters and proper deserialization:

- **CLI Tool**: Interactive Kusto CLI with query execution, schema export, cluster management, database/table browsing, and multiple output formats.
- **Kusto Query and Command Execution**: Simplifies the execution of Kusto queries and commands with asynchronous support through embedded .kusto scripts.
- **Decimal Type Deserialization**: Seamless handling of ADX decimal values (including those surfaced via structured SqlDecimal representations) through internal custom JSON converters bridging Newtonsoft.Json and System.Text.Json.
- **Paged Query Support**: Efficient handling of large datasets with built-in support for paginated query results through stored query results.
- **Streaming Query Support**: Two approaches for streaming large result sets:
  - **Direct Streaming**: Immediately yield rows as they become available, minimizing memory usage and latency.
  - **Buffered Streaming**: Buffer results with additional metadata like schemas and completion information.
- **Data Ingestion**: `IKustoIngestor` ingests in-memory rows, streams and blobs with streaming, managed-streaming (default) or queued ingestion, built on the Kusto Ingest V2 SDK.
- **Health Checks**: Built-in health check integration for Azure Data Explorer clusters with ASP.NET Core's Health Checks API.

## CLI Tool

The `atc-kusto` CLI tool provides interactive Kusto query execution, schema export, cluster management, and database/table browsing with multiple output formats.

### Installation

```bash
dotnet tool install --global atc-kusto
```

Or run directly from the project:

```bash
dotnet run --project src/Atc.Kusto.CLI
```

### Quick Start

```bash
# 1) Save a cluster connection (first cluster becomes default automatically)
atc-kusto cluster add help https://help.kusto.windows.net --use

# 2) Set a default database for that cluster
atc-kusto database set-default Samples --tenant-id <GUID> --cluster help

# 3) Run a query (uses saved defaults - no --cluster-url or --database needed)
atc-kusto query "StormEvents | take 5" --tenant-id <GUID>

# Output as CSV and redirect to a file
atc-kusto query "StormEvents | summarize Count=count() by State | top 10 by Count desc" \
  --tenant-id <GUID> --format csv > top-states.csv

# Output as TSV and redirect to a file
atc-kusto query "StormEvents | summarize Count=count() by State | top 10 by Count desc" \
  --tenant-id <GUID> --format tsv > top-states.tsv

# Run a query from a file
atc-kusto query --file myquery.kql --tenant-id <GUID>

# Run specific lines from a query file
atc-kusto query --file queries.kql:5-10 --tenant-id <GUID>
```

### Authentication

The CLI uses `DefaultAzureCredential` from Azure Identity, scoped to the tenant specified via `--tenant-id`. This supports multiple authentication methods including:

- Azure CLI (`az login --tenant <GUID>`)
- Visual Studio / VS Code credentials
- Managed Identity (when running in Azure)
- Environment variables

Ensure you are authenticated before running commands, for example via `az login --tenant <GUID>`.

### Configuration

The CLI stores saved clusters and default databases at:

- **Default**: `{LocalApplicationData}/atc-kusto/config.json`
  - Windows: `%LOCALAPPDATA%\atc-kusto\config.json`
  - Linux/macOS: `~/.local/share/atc-kusto/config.json`
- **Override**: Set the `KUSTO_CLI_CONFIG_PATH` environment variable

### Cluster Management

Save cluster connections by name so you can reference them with `--cluster` instead of typing full URLs.

| Command | Description |
|---------|-------------|
| `cluster list` | List all saved clusters |
| `cluster show <name>` | Show details for a saved cluster |
| `cluster add <name> <url>` | Save a new cluster connection (`--use` to set as default) |
| `cluster remove <name>` | Remove a saved cluster |
| `cluster set-default <name>` | Set the default cluster |

```bash
# Add a cluster and set it as default
atc-kusto cluster add prod https://prod.kusto.windows.net --use

# List saved clusters
atc-kusto cluster list

# Switch default cluster
atc-kusto cluster set-default staging
```

### Connection Options

All commands that connect to a cluster accept these options:

| Option | Description |
|--------|-------------|
| `--tenant-id <GUID>` | **(Required)** Azure AD tenant ID |
| `--cluster-url <URL>` | Kusto cluster URL (e.g. `https://mycluster.kusto.windows.net`) |
| `--cluster <NAME>` | Saved cluster name (from `cluster add`) |
| `--database <NAME>` | Database name |

You must provide either `--cluster-url` or `--cluster`, or have a default cluster configured. Similarly, `--database` can be omitted if a default database is set for the cluster.

### Query Command

Execute KQL queries against a Kusto database with inline text, files, or stdin.

```bash
# Inline query
atc-kusto query "StormEvents | take 5" --tenant-id <GUID> --cluster prod --database Samples

# From a file
atc-kusto query --file myquery.kql --tenant-id <GUID> --cluster prod --database Samples

# From specific lines in a file (line range syntax: path:start-end)
atc-kusto query --file queries.kql:12-15 --tenant-id <GUID> --cluster prod --database Samples

# From stdin
echo "StormEvents | count" | atc-kusto query - --tenant-id <GUID> --cluster prod --database Samples
```

#### Query Options

| Option | Description |
|--------|-------------|
| `[QUERY]` | Inline KQL query text, or `-` to read from stdin |
| `--file\|-f <PATH>` | Read query from a file (supports `:start-end` line range) |
| `--format <FORMAT>` | Output format: `human`, `json`, `markdown` (or `md`), `csv`, `tsv` (default: `human`) |
| `--show-stats` | Include query execution statistics in output |

#### Output Formats

| Format | Description |
|--------|-------------|
| `human` | Spectre.Console table with ANSI colors and borders (default) |
| `json` | JSON array for scripting and automation |
| `markdown` | GitHub Flavored Markdown table (`md` is accepted as an alias) |
| `csv` | Comma-separated values (RFC 4180 quoting) |
| `tsv` | Tab-separated values |

```bash
# JSON output for scripting
atc-kusto query "StormEvents | take 5" --format json --tenant-id <GUID>

# Markdown output
atc-kusto query "StormEvents | take 5" --format markdown --tenant-id <GUID>

# CSV output redirected to file
atc-kusto query "StormEvents | take 5" --format csv --tenant-id <GUID> > results.csv

# TSV output redirected to file
atc-kusto query "StormEvents | take 5" --format tsv --tenant-id <GUID> > results.tsv
```

#### Query Validation

Queries are validated locally using the KQL parser before being sent to the server. Syntax errors are reported with line and column numbers without a network round-trip.

#### Query Statistics

Use `--show-stats` to display execution statistics after query results. Statistics include CPU time, memory usage, cache hit/miss ratios, extents scanned, and result size.

```bash
atc-kusto query "StormEvents | count" --show-stats --tenant-id <GUID>
```

> Note: `--show-stats` cannot be used with `--format csv` or `--format tsv`.

#### Web Explorer Link

For recognized Azure cloud clusters, query results include a deep-link URL to open the query in the Azure Data Explorer web UI. The query is GZip-compressed and Base64-encoded in the URL.

Supported clouds: Public (`.kusto.windows.net`, `.kusto.data.microsoft.com`, `.kusto.fabric.microsoft.com`), US Government (`.kusto.usgovcloudapi.net`), China (`.kusto.chinacloudapi.cn`).

### Database Commands

| Command | Description |
|---------|-------------|
| `database list` | List databases in a cluster |
| `database show <name>` | Show details for a database (accepts `--format`) |
| `database set-default <name>` | Set the default database for a cluster |

Available options for `database list`:

| Option | Description |
|--------|-------------|
| `--format <FORMAT>` | Output format: `human`, `json`, `markdown` (`md` is accepted as an alias) (default: `human`) |
| `--filter <PATTERN>` | Filter databases by name. Supports `^prefix`, `suffix$`, `^exact$`, or plain substring match |
| `--take <N>` | Limit the number of results returned |

```bash
# List databases (with optional filter)
atc-kusto database list --tenant-id <GUID> --cluster prod
atc-kusto database list --filter "^prod" --tenant-id <GUID> --cluster prod

# Set default database for a cluster
atc-kusto database set-default Samples --tenant-id <GUID> --cluster help
```

### Table Commands

| Command | Description |
|---------|-------------|
| `table list` | List tables in a database |
| `table show <name>` | Show table schema and column details (accepts `--format`) |

Available options for `table list`:

| Option | Description |
|--------|-------------|
| `--format <FORMAT>` | Output format: `human`, `json`, `markdown` (`md` is accepted as an alias) (default: `human`) |
| `--filter <PATTERN>` | Filter tables by name. Supports `^prefix`, `suffix$`, `^exact$`, or plain substring match |
| `--take <N>` | Limit the number of results returned |

```bash
# List tables (with optional filter and limit)
atc-kusto table list --tenant-id <GUID> --cluster prod --database MyDb
atc-kusto table list --filter "Storm" --take 10 --tenant-id <GUID> --cluster prod --database MyDb

# Show table schema
atc-kusto table show StormEvents --tenant-id <GUID> --cluster prod --database Samples
```

### Export Commands

Export Azure Data Explorer database schemas as `.kql` files for version control and review.

| Command | Description |
|---------|-------------|
| `export schema` | Export the full database schema (tables, functions, materialized views, external tables, and policies) |
| `export tables` | Export table schemas only |
| `export functions` | Export functions only |
| `export materialized-views` | Export materialized views only |
| `export external-tables` | Export external tables only |
| `export policies` | Export retention and caching policies only |

```bash
atc-kusto export schema \
  --tenant-id <GUID> \
  --cluster-url https://mycluster.kusto.windows.net \
  --database MyDatabase \
  --output-dir ./kusto-schema
```

This produces the following directory structure:

```
kusto-schema/
├── Tables/
│   └── MyTable.kql
├── Functions/
│   └── MyFunction.kql
├── MaterializedViews/
│   └── DailySales.kql
├── ExternalTables/
│   └── ExternalLogs.kql
└── Policies/
    ├── Database_RetentionPolicy.kql
    ├── Database_CachingPolicy.kql
    ├── MyTable_RetentionPolicy.kql
    └── MyTable_CachingPolicy.kql
```

Additional export options:

| Option | Description |
|--------|-------------|
| `--output-dir <PATH>` | Output directory (defaults to current directory) |

> All export commands also accept the [connection options](#connection-options) (`--tenant-id`, `--cluster-url`, `--cluster`, `--database`).

## Getting started

### Configuring the Atc.Kusto library using ServiceCollection Extensions

To seamlessly integrate Azure Data Explorer (Kusto) services into your application, you can utilize the provided `ServiceCollection` extension methods. These methods simplify the setup process and ensure that the Kusto services are correctly configured and ready to use within your application's service architecture.

The extension methods allow you to configure Kusto services using different approaches — explicit parameters, a pre-configured `AtcKustoOptions` instance, or an `Action<AtcKustoOptions>` delegate for dynamic configuration.

All methods ensure that the Kusto services are added to the application's service collection and configured according to the specified parameters, making them available throughout your application via dependency injection.

#### Setup with Explicit Parameters

If you prefer to configure Kusto services with explicit values for the cluster's host address, database name, and credentials, you can use the following approach:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureAzureDataExplorer(
    new Uri(builder.Configuration["Kusto:HostAddress"]),
    builder.Configuration["Kusto:DatabaseName"],
    new DefaultAzureCredential());
```

#### Setup with Pre-Configured Options

When you already have a pre-configured AtcKustoOptions instance, you can directly pass it to the configuration method:

```csharp
var builder = WebApplication.CreateBuilder(args);

var kustoOptions = new AtcKustoOptions
{
    HostAddress = builder.Configuration["Kusto:HostAddress"],
    DatabaseName = builder.Configuration["Kusto:DatabaseName"],
    Credential = new DefaultAzureCredential(),
};

builder.Services.ConfigureAzureDataExplorer(kustoOptions);
```

#### Setup with Configuration Delegate

For more flexibility, you can configure Kusto services using an Action<AtcKustoOptions> delegate. This is particularly useful when you need to dynamically adjust settings during application startup:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureAzureDataExplorer(options =>
{
    options.HostAddress = builder.Configuration["Kusto:HostAddress"];
    options.DatabaseName = builder.Configuration["Kusto:DatabaseName"];
    options.Credential = new DefaultAzureCredential();
});
```

### Adding a Kusto query

A Kusto query can be added by creating two files in your project:

- A `.kusto` script file containing the Kusto query itself (with "Build Action" set to "Embedded resource")
- A .NET record with the same name (and namespace) as the embedded `.kusto` script.

The .NET record should to derive from one of the following base types:

| Base type       | Description                                            |
| --------------- | ------------------------------------------------------ |
| `KustoCommand`  | Used for Kusto commands that do not produce an output. |
| `KustoQuery<T>` | Used for Kusto queries that returns a result.          |

> Note: The base types handles the loading of the embedded `.kusto` script file, passing of parameters and deserialization of the output._

#### Required Project Configuration

To enable compile-time validation of .kusto files, you **must** configure your `.csproj` to include .kusto files as both embedded resources and additional files:

```xml
<ItemGroup>
  <!-- Required for runtime: embeds .kusto files in the assembly -->
  <EmbeddedResource Include="**/*.kusto" />

  <!-- Required for compile-time validation: allows the analyzer to verify .kusto files exist -->
  <AdditionalFiles Include="**/*.kusto" />
</ItemGroup>
```

**What this enables:**

- ✅ **Compile-time errors** if a .kusto file is missing for a KustoScript class
- ✅ **Code fix** that creates stub .kusto files with example queries
- ✅ **Build failures** prevent runtime errors from missing query files

Without the `<AdditionalFiles>` configuration, the compiler will report errors (ATCK301) even if the .kusto files exist, because the analyzer cannot see them during compilation.

#### Defining Query Parameters

Parameters are specified by adding them to record, and declare them at the top of the `.kusto` script, like this:

```csharp
// file: GetTeamQuery.cs
public record GetTeamQuery(long TeamId)
    : KustoScript, IKustoQuery<Team>
{
    public Team? ReadResult(IDataReader reader)
        => reader.ReadObjects<Team>().FirstOrDefault();
}
```

```kusto
// file: GetTeamQuery.kusto
declare query_parameters (
    teamId:long)
;
Teams
| where entityId == teamId
| project
    Id = tolong(payload.id),
    Name = tostring(payload.name)
```

The query result is mapped to the specified output contract, by matching parameter names like this:

```csharp
// file: Team.cs
public record Team(
    string Id,
    string Name);
```

> Note: The above example in GetTeamQuery.cs is used to directly override the ReadResults method, if this is not needed, simply inherit directly from KustoQuery and accept the default implementation of the ReadResult method.

```csharp
public record GetTeamQuery(long TeamId)
    : KustoQuery<Team>;
```

### Kusto query examples

The following examples demonstrate different types of queries, showcasing single result queries, list queries, and more complex queries with multiple result sets.

#### Single

> The following C# record is defined in the [CustomerByIdQuery.cs](./sample/Atc.Kusto.Sample/Queries/CustomerByIdQuery.cs) file:

```csharp
public record CustomerByIdQuery(long CustomerId)
    : KustoQuery<Customer>;
```

> The following KQL query is defined in the [CustomerByIdQuery.kusto](./sample/Atc.Kusto.Sample/Queries/CustomerByIdQuery.kusto) file:

```kusto
declare query_parameters (
    customerId:long
);
Customers
| where customerId == CustomerKey
| project
    CustomerKey,
    FirstName,
    LastName,
    CompanyName,
    CityName,
    StateProvinceName,
    RegionCountryName,
    ContinentName,
    Gender,
    MaritalStatus,
    Education,
    Occupation
```

#### List

> The following C# record is defined in the [CustomerSalesQuery.cs](./sample/Atc.Kusto.Sample/Queries/CustomerSalesQuery.cs) file:

```csharp
public record CustomerSalesQuery
    : KustoQuery<CustomerSales>;
```

> The following KQL query is defined in the [CustomerSalesQuery.kusto](./sample/Atc.Kusto.Sample/Queries/CustomerSalesQuery.kusto) file:

```kusto
Customers
| join kind=inner SalesFact on CustomerKey
| extend CustomerName = strcat(FirstName, ' ', LastName)
| summarize
    SalesAmount = round(sum(SalesAmount), 2),
    TotalCost = round(sum(TotalCost), 2)
  by CustomerKey, CustomerName
```

#### Complex with multiple result sets

> The following C# record is defined in the [CustomersSplitByGenderQuery.cs](./sample/Atc.Kusto.Sample/Queries/CustomersSplitByGenderQuery.cs) file:

```csharp
public record CustomersSplitByGenderQuery
    : KustoScript, IKustoQuery<CustomersByGender>
{
    public CustomersByGender ReadResult(IDataReader reader)
        => new(
            reader.ReadObjects<Customer>(),
            reader.ReadObjectsFromNextResult<Customer>(),
            reader.ReadObjectsFromNextResult<CustomerGenderCount>());
}
```

> The following KQL query is defined in the [CustomersSplitByGenderQuery.kusto](./sample/Atc.Kusto.Sample/Queries/CustomersSplitByGenderQuery.kusto) file:

```kusto
// Create materialized result with rows from customers
let customers = materialize(Customers
| project
    CustomerKey,
    FirstName,
    LastName,
    CompanyName,
    CityName,
    StateProvinceName,
    RegionCountryName,
    ContinentName,
    Gender,
    MaritalStatus,
    Education,
    Occupation)
;
// Female Customers
customers
| where Gender == "F"
;
// Male Customers
customers
| where Gender == "M"
;
// Customer count by gender
customers
| summarize Count = count() by Gender
```

### Executing a Kusto query

Kusto scripts can be executed using the `IKustoProcessor` registered in the DI container, like this:

```csharp
app.MapGet(
    "/customers/{customerId}",
    async static (
        int customerId,
        IKustoProcessor processor,
        CancellationToken cancellationToken)
        => (IResult)(await processor.ExecuteQuery(
            new CustomersQuery(customerId),
            cancellationToken)
            switch
            {
                [{ } customer] => TypedResults.Ok((object?)customer),
                _ => TypedResults.NotFound(),
            }))
    .WithName("GetCustomerById")
    .WithOpenApi();
```

The processor can also perform pagination by using the `ExecutePagedQuery` overload, taking in a session id, a continuation token and a max item count, like this:

```csharp
app.MapGet(
    "/customers",
    async static (
        [FromHeader(Name = "x-client-session-id")] string? sessionId,
        [FromHeader(Name = "x-pageSize")] int? pageSize,
        [FromHeader(Name = "x-continuation-token")] string? continuationToken,
        IKustoProcessor processor,
        CancellationToken cancellationToken)
        => await processor.ExecutePagedQuery(
            new CustomersQuery(),
            sessionId,
            pageSize ?? 100,
            continuationToken,
            cancellationToken))
    .WithName("GetCustomers")
    .WithOpenApi();
```

The `pageSize` specifies how many items to return for each page. Each page is returned with a `continuationToken` that can be specified to fetch the next page.

The optional `sessionId` can be provided to optimize the use of storage on the ADX. If the same `sessionId` is specified for two calls they will share the underlying storage for pagination results.

#### Choosing the connection and database per call

The injected `IKustoProcessor` uses the default connection and its configured `DatabaseName`. To target another named connection or another database on the same cluster, create a processor with `IKustoProcessorFactory`:

```csharp
// Another database on the default connection
var archive = processorFactory.Create(databaseName: "ArchiveDb");

// A named connection, using its configured DatabaseName
var reporting = processorFactory.Create(connectionName: "Reporting");

// A named connection and a specific database
var reportingArchive = processorFactory.Create("Reporting", "ArchiveDb");

var customers = await archive.ExecuteQuery(new CustomersQuery(), cancellationToken);
```

The database is resolved in this order, for both `HostAddress` and `ConnectionString` configurations:

1. the `databaseName` passed to `Create(...)`;
2. the connection's `AtcKustoOptions.DatabaseName`;
3. for a `ConnectionString` only: the database in the connection string itself (`Initial Catalog=...`), otherwise the SDK default.

A `HostAddress` connection with neither 1 nor 2 throws `InvalidOperationException` on first use.

### Executing streaming queries

Streaming queries allow you to process large result sets more efficiently by streaming results as they become available. Atc.Kusto provides two approaches for streaming:

#### Direct streaming

Direct streaming yields rows immediately as they are processed from Kusto, providing the lowest latency and minimal memory usage. This approach is suitable when you need to process a large number of results as quickly as possible and don't require metadata about the query execution:

```csharp
// Define your streaming query
public record CustomersStreamingQuery()
    : KustoStreamingQuery<Customer>;
```

```csharp
// Execute the streaming query and process results as they arrive
await foreach (var customer in kustoProcessor.ExecuteStreamingQuery(
    new CustomersStreamingQuery(), 
    cancellationToken))
{
    // Process each customer as it arrives
    Console.WriteLine($"{customer.FirstName} {customer.LastName}");
}
```

#### Buffered streaming

Buffered streaming provides additional metadata like table schemas and completion information, while still allowing you to stream the results:

```csharp
// Execute buffered streaming query
var streamingResult = await kustoProcessor.ExecuteBufferedStreamingQuery(
    new CustomersStreamingQuery(),
    cancellationToken);

// Access metadata if needed
Console.WriteLine($"Has errors: {streamingResult.Completion?.HasErrors}");

// Stream the results
await foreach (var customer in streamingResult.Rows.WithCancellation(cancellationToken))
{
    // Process each customer
    Console.WriteLine($"{customer.FirstName} {customer.LastName}");
}
```

In a web API scenario, you can return the stream directly to the client:

```csharp
app.MapGet(
    "/customers/stream",
    (IKustoProcessorFactory processorFactory, CancellationToken cancellationToken) => 
        Task.FromResult(processorFactory.Create(databaseName: "DatabaseName")
            .ExecuteStreamingQuery(
                new CustomersStreamingQuery(),
                cancellationToken)))
    .WithName("GetCustomersStream");
```

This returns a streamed response to the client, which can be processed as it arrives.

### Ingesting data

`IKustoIngestor` writes data into a Kusto table. It is registered automatically by `ConfigureAzureDataExplorer(...)` — there is no separate opt-in — and supports three sources: in-memory rows, a `Stream`, and a blob.

> **Requirements**
>
> - The connection must be configured with **both `HostAddress` and a `Credential`**. A `ConnectionString`-only or credential-less connection can query but not ingest; the first ingest call throws `InvalidOperationException`.
> - The target table must exist. For JSON payloads it also needs a **JSON ingestion mapping**, referenced by name.
> - Real streaming needs streaming ingestion enabled on the cluster and a streaming ingestion policy on the table or database (`.alter table MyTable policy streamingingestion enable`).

#### Ingestion quick start

```kusto
// One-time table setup on the cluster
.create table DeviceReadings (deviceId: string, serialNumber: string, value: real, timestamp: datetime)

.create table DeviceReadings ingestion json mapping 'DeviceReadings_mapping'
'[{"column":"deviceId","path":"$.deviceId"},{"column":"serialNumber","path":"$.serialNumber"},{"column":"value","path":"$.value"},{"column":"timestamp","path":"$.timestamp"}]'

.alter table DeviceReadings policy streamingingestion enable
```

```csharp
public sealed record DeviceReading(string DeviceId, string SerialNumber, double Value, DateTimeOffset Timestamp);

public sealed class ReadingsWriter(IKustoIngestor ingestor)
{
    public async Task WriteAsync(IReadOnlyList<DeviceReading> readings, CancellationToken cancellationToken)
    {
        var result = await ingestor.IngestAsync(
            readings,
            new KustoIngestTarget
            {
                TableName = "DeviceReadings",
                Format = KustoIngestFormat.MultiJson,
                MappingReference = "DeviceReadings_mapping",
            },
            cancellationToken: cancellationToken);

        result.EnsureSuccess(); // throws KustoIngestionException if the ingestion failed
    }
}
```

#### Checking the result

> **Failures are returned, not thrown.** When the cluster rejects the data, or a network or credential problem occurs, `IngestAsync` completes normally with `Status = Failed` and an `ErrorMessage`. Code that ignores the result will not notice the failure (it is still logged at error level). **Always check `result.IsSuccess`, or call `result.EnsureSuccess()`** — it works like `HttpResponseMessage.EnsureSuccessStatusCode()`.

| `Status`    | Meaning                                                                                  | `IsSuccess` |
| ----------- | ---------------------------------------------------------------------------------------- | ----------- |
| `Succeeded` | Streamed: the rows are in the table when the call returns.                               | `true`      |
| `Queued`    | Accepted and queued; the cluster batches it and it becomes queryable later (typically minutes). | `true` |
| `Skipped`   | Nothing to send (no rows, or an empty stream); the cluster was not contacted.            | `true`      |
| `Failed`    | The ingestion failed; see `ErrorMessage`.                                                | `false`     |

Only these throw:

| Exception                     | When                                                                                                    |
| ----------------------------- | ------------------------------------------------------------------------------------------------------- |
| `ArgumentException`           | Invalid request: missing table name, JSON format without `MappingReference`, no resolvable database, a non-seekable stream, a relative blob URI, more than 10 MB for `Streaming`. Validated before any data is read. |
| `InvalidOperationException`   | The connection has no `HostAddress` or `Credential`.                                                    |
| `OperationCanceledException`  | Your `CancellationToken` was cancelled. The original exception, if any, is the `InnerException`.         |

`KustoIngestionResult` also carries `Mode` (the resolved mode — the target's `Mode` or the connection default; after a managed fallback it is still `ManagedStreaming`, while `Status` says `Queued`), `OperationId` (the service's id for the operation — useful for correlating with cluster logs) and `OperationHandle` (see [Tracking](#tracking-queued-ingestion)).

#### Ingestion modes

| Mode                          | What happens                                                                                       | Status you get        |
| ----------------------------- | -------------------------------------------------------------------------------------------------- | --------------------- |
| `ManagedStreaming` (default)  | Tries streaming; falls back to queued when the payload is too large, after repeated transient errors, or when streaming isn't enabled for the table. | `Succeeded` or `Queued` |
| `Streaming`                   | Sends directly; fails instead of falling back.                                                     | `Succeeded` / `Failed` |
| `Queued`                      | Uploads and queues for batched ingestion.                                                          | `Queued`              |

Which one to use:

- **`ManagedStreaming`** (default) for application writes: small batches become visible immediately, and it never fails just because of size or table configuration.
- **`Queued`** for bulk loads (backfills, nightly imports): highest throughput, and no pointless streaming attempt for large payloads.
- **`Streaming`** when the data *must* be queryable right after the call or the call must fail — e.g. tests or read-your-own-write flows.

Set the mode per call (`KustoIngestTarget.Mode`) or per connection (`AtcKustoOptions.DefaultIngestionMode`):

```csharp
// Per call: this one ingestion is queued, whatever the connection default is
await ingestor.IngestAsync(
    backfillRows,
    new KustoIngestTarget
    {
        TableName = "DeviceReadings",
        Format = KustoIngestFormat.MultiJson,
        MappingReference = "DeviceReadings_mapping",
        Mode = IngestionMode.Queued,
    },
    cancellationToken: cancellationToken);
```

```csharp
// Per connection: every ingestion on this connection without an explicit Mode is queued
builder.Services.ConfigureAzureDataExplorer(options =>
{
    options.HostAddress = new Uri("https://mycluster.westeurope.kusto.windows.net");
    options.DatabaseName = "Telemetry";
    options.Credential = new DefaultAzureCredential();
    options.DefaultIngestionMode = IngestionMode.Queued;
});
```

A `Mode` set on the target always wins over the connection default.

> Streaming limits: the client rejects `Streaming` payloads over **10 MB** up front, but the service's data limit for a streaming request is **4 MB** (including data produced by update policies), so plain `Streaming` can still fail between 4 and 10 MB. `ManagedStreaming` handles this by falling back to queued.

#### JSON property names and ingestion mappings

In-memory rows are serialized to multijson with **camelCase property names**: `SerialNumber` becomes `"serialNumber"`. The table's JSON mapping paths are **case-sensitive** and must match:

```kusto
'[{"column":"serialNumber","path":"$.serialNumber"}]'   // ✅ matches
'[{"column":"serialNumber","path":"$.SerialNumber"}]'   // ❌ ingests successfully, but the column is left empty
```

A mismatch does **not** fail the ingestion — the rows arrive with empty columns. To use other names, annotate properties with `[JsonPropertyName("...")]`, or pass `serializerOptions` to `IngestAsync` (note: supplied options replace the library defaults, including enum-as-string and the Kusto `bool`/`DateOnly` handling).

Rename a single property — everything else stays camelCase:

```csharp
public sealed record DeviceReading(
    [property: JsonPropertyName("device_id")] string DeviceId,   // written as "device_id"
    string SerialNumber,                                         // written as "serialNumber"
    double Value,
    DateTimeOffset Timestamp);
```

Change the naming for the whole call, e.g. for a table whose mapping uses snake_case paths (`$.serial_number`):

```csharp
var snakeCase = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, // SerialNumber → "serial_number"
    Converters = { new JsonStringEnumConverter() },         // re-add what you still need: these options replace the defaults
};

await ingestor.IngestAsync(readings, target, serializerOptions: snakeCase, cancellationToken: cancellationToken);
```

#### Ingesting a stream or a blob

```csharp
// A CSV file (or any seekable stream). The stream is not closed; data from its current position is ingested.
await using var csv = File.OpenRead("readings.csv");
var result = await ingestor.IngestAsync(
    csv,
    new KustoIngestTarget { TableName = "DeviceReadings", Format = KustoIngestFormat.Csv },
    cancellationToken);

// A blob the cluster reads itself — it must be readable by the cluster (SAS token or RBAC).
// Compression is inferred, e.g. from a .gz extension.
var blobResult = await ingestor.IngestFromBlobAsync(
    new Uri("https://account.blob.core.windows.net/exports/readings.csv.gz?sv=...&sig=..."),
    new KustoIngestTarget { TableName = "DeviceReadings", Format = KustoIngestFormat.Csv, Mode = IngestionMode.Queued },
    cancellationToken);
```

Streams must be seekable so their size can be validated; copy a forward-only stream (e.g. a network stream) into a `MemoryStream` or temporary file first. CSV and TSV may omit `MappingReference` (columns are matched by position).

#### `KustoIngestTarget`

| Property           | Required | Description                                                                                     |
| ------------------ | -------- | ----------------------------------------------------------------------------------------------- |
| `TableName`        | yes      | Destination table.                                                                              |
| `Format`           | yes      | `MultiJson`, `Json`, `Csv` or `Tsv`. In-memory rows require `MultiJson` or `Json`.              |
| `MappingReference` | JSON     | Name of the table's ingestion mapping. Required for `Json`/`MultiJson`, optional for CSV/TSV.   |
| `Mode`             | no       | Overrides `AtcKustoOptions.DefaultIngestionMode` (default `ManagedStreaming`).                  |
| `ConnectionName`   | no       | Named connection; `null` uses the default connection.                                           |
| `DatabaseName`     | no       | Overrides the connection's `DatabaseName`.                                                      |
| `EnableTracking`   | no       | Request an operation handle (queued/managed only; rejected for pure `Streaming`).               |

#### Tracking queued ingestion

With `EnableTracking = true` (queued or managed-streaming only), `result.OperationHandle` contains a serialized operation handle that identifies the queued ingestion. Store it to check the ingestion's outcome later:

```csharp
var result = await ingestor.IngestAsync(
    rows,
    new KustoIngestTarget
    {
        TableName = "DeviceReadings",
        Format = KustoIngestFormat.MultiJson,
        MappingReference = "DeviceReadings_mapping",
        Mode = IngestionMode.Queued,
        EnableTracking = true,
    },
    cancellationToken: cancellationToken);

logger.LogInformation("Queued ingestion {OperationId}, handle {OperationHandle}", result.OperationId, result.OperationHandle);
```

Later — in the same process or another one, e.g. a background job that stored the handle — ask for its state with `GetIngestionStatusAsync`. Pass the same connection name the ingestion used (the handle identifies database and table, not the cluster):

```csharp
var status = await ingestor.GetIngestionStatusAsync(result.OperationHandle!, cancellationToken: cancellationToken);

while (!status.IsCompleted)
{
    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
    status = await ingestor.GetIngestionStatusAsync(result.OperationHandle!, cancellationToken: cancellationToken);
}

if (status.Status != KustoIngestionOperationStatus.Succeeded)
{
    foreach (var error in status.Errors)
    {
        logger.LogError("Ingestion {OperationId} failed: {ErrorCode} (transient: {IsTransient}) {Details}",
            status.OperationId, error.ErrorCode, error.IsTransient, error.Details);
    }
}
```

| `Status`         | Meaning                                                          | `IsCompleted` |
| ---------------- | ---------------------------------------------------------------- | ------------- |
| `InProgress`     | Still being batched/ingested — check again later.                | `false`       |
| `Succeeded`      | All data ingested and queryable.                                 | `true`        |
| `PartialSuccess` | Some sources ingested, some failed — see `Errors`.               | `true`        |
| `Failed`         | Ingestion failed — see `Errors` (`ErrorCode`, `IsTransient`, `Details`). | `true` |
| `Cancelled`      | The service cancelled the ingestion.                             | `true`        |

Unlike the ingest methods, a failure to **check** the status (cluster unreachable, credential failure) throws `KustoIngestionException` — the check only reads, so it is safe to retry. An ingestion that was streamed (managed streaming without fallback) reports `Succeeded` immediately without contacting the cluster.

#### Retries and duplicates

Kusto ingestion is **at-least-once**. A `Failed` result means the library did not get a confirmed answer — after a timeout or dropped connection the data **may still have been written**. If you retry failed ingestions, design for duplicates:

- **Deduplicate on read** with a materialized view keyed on an id your rows carry — for example keep the latest version per key:

  ```kusto
  .create materialized-view with (backfill=true) LatestDeviceReadings on table DeviceReadings
  {
      DeviceReadings
      | summarize arg_max(timestamp, *) by deviceId, serialNumber
  }
  ```

- For **infrequent, queued** batch loads only, Kusto's `ingest-by:` extent tags with `ingestIfNotExists` can skip a batch that already landed. They aren't supported for streaming ingestion, and Microsoft warns that a unique tag per call hurts performance, so don't use them for frequent telemetry batches.

The library itself does not retry ingestion on top of the SDK: the Ingest V2 SDK already retries transient failures, and a second retry layer would only add duplicates.

#### Private endpoints: upload containers

Queued and managed-streaming ingestion upload the payload to blob storage first. By default that is Kusto's own internal storage. For clusters behind a private endpoint, supply your own privately reachable containers:

```csharp
builder.Services.ConfigureAzureDataExplorer(options =>
{
    options.HostAddress = new Uri("https://mycluster.westeurope.kusto.windows.net");
    options.DatabaseName = "Telemetry";
    options.Credential = new DefaultAzureCredential();
    options.IngestUploadContainers.Add(new Uri("https://myprivatestorage.blob.core.windows.net/kusto-ingest"));
});
```

The connection's `Credential` is used for the containers too, so that identity needs **Storage Blob Data Contributor** on them. The credential is asked for a token the first time a client for that connection and mode is created; a credential failure there is reported as a `Failed` result.

#### Dependencies

Ingestion is built on the **Kusto Ingest V2 SDK** (`Microsoft.Azure.Kusto.Ingest.V2`), which is still a pre-1.0 preview. It is a dependency of the Atc.Kusto package, but no V2 type appears in Atc.Kusto's public API, so SDK changes stay internal to the library.

Atc.Kusto works around these V2 SDK (0.0.9) behaviours for you — worth knowing if you also use the SDK directly:

- **Operation handles and culture:** `IngestionOperation.ToJsonString()` writes the operation's start time with the machine's time separator (`20.33.00` on Danish regional settings) and `FromJsonString` can't read it back, so such a handle is unreadable everywhere. Atc.Kusto writes and reads handles with the invariant culture. The same operation serialized by the SDK on two machines:

  ```text
  en-US / invariant culture:
  {"id":"op-123","database":"Db","table":"T","startTime":"2026-10-09T20:33:18.8657719Z","storedResults":[], ...}
                                                                       ^  ^  ':' time separators -> readable

  da-DK / en-DK regional settings - FromJsonString throws FormatException:
  {"id":"op-123","database":"Db","table":"T","startTime":"2026-10-09T20.33.18.8129343Z","storedResults":[], ...}
                                                                       ^  ^  '.' time separators -> unreadable
  ```
- **Managed streaming fallback:** the SDK's `ManagedStreamingPolicy` does not fall back to queued ingestion when streaming is disabled for a table (`ContinueWhenStreamingIngestionUnavailable = false`). Atc.Kusto enables the fallback, so `ManagedStreaming` behaves as described above.
- **Uploader lifetime:** the managed streaming client never disposes a custom uploader. Atc.Kusto owns and disposes it.

### Health Checks

The library provides built-in health check support for Azure Data Explorer clusters, which can be easily integrated with ASP.NET Core's Health Checks API. This feature allows you to monitor the health of your Kusto clusters and integrate it with your application's monitoring infrastructure.

#### Setup Health Check

To add a Kusto cluster health check to your application, use the `AddKustoHealthCheck` extension method:

```csharp
// Configure health checks
builder.Services
    .AddHealthChecks()
    .AddKustoHealthCheck(
        name: "adx",  // Optional: Name for the health check
        connectionName: "DefaultConnection",  // Optional: Connection name to use
        databaseName: null,  // Optional: Database name
        tags: new[] { "adx", "database" });  // Optional: Tags
```

The health check will execute a `.show diagnostics` query against the cluster to retrieve health information.

#### Health Check Response

The health check returns detailed health information about your Kusto cluster:

- **IsHealthy**: Whether the cluster is functioning normally
- **NotHealthyReason**: If unhealthy, why the cluster is not healthy
- **IsAttentionRequired**: Whether the cluster requires attention
- **AttentionRequiredReason**: If attention is required, why it's required
- **IsScaleOutRequired**: Whether it's recommended to scale out the cluster

#### Health Check Statuses

The health check maps cluster health to ASP.NET Core health statuses:

- **Healthy**: The cluster is functioning normally
- **Degraded**: The cluster requires attention but is still operational
- **Unhealthy**: The cluster is not healthy and may not be operational

#### Using Health Check Programmatically

You can also use the `IKustoHealthCheck` interface directly in your code:

```csharp
public class MyService
{
    private readonly IKustoHealthCheck healthCheck;

    public MyService(IKustoHealthCheck healthCheck)
    {
        this.healthCheck = healthCheck;
    }

    public async Task CheckClusterHealth()
    {
        var result = await healthCheck.CheckHealthAsync("MyConnection");

        if (!result.IsHealthy)
        {
            // Handle unhealthy cluster scenario
            Console.WriteLine($"Cluster unhealthy: {result.NotHealthyReason}");
        }
    }
}
```

For more details, check the [health check sample](./sample/Atc.Kusto.HealthCheck.Sample/).

## Sample

See the [sample api](./sample/Atc.Kusto.Api.Sample/) for an example on how to configure the Atc.Kusto library. Also see the [sample console application](./sample/Atc.Kusto.Sample/) for an example of utilizing the library directly without being wrapped in an API.

Both samples are querying the "ContosoSales" database of the Microsoft ADX sample cluster.

## Retries

Queries (`ExecuteQuery`, and the follow-up pages of `ExecutePagedQuery`) are retried automatically: up to **3 retries**, waiting 3, 6 and 12 seconds. Each retry is logged as a warning.

Only failures that can succeed on a later attempt are retried:

| Retried | Not retried |
|---|---|
| Throttling, service timeouts, service unavailable, connection and network failures | Syntax and semantic errors (e.g. a misspelled column), database or table not found, access denied by the cluster |
| Sign-in failures (getting a token), e.g. a managed identity that is not ready yet right after a deploy | Errors in your own code, such as a result that cannot be mapped to your type |
| Any other Kusto error the SDK marks as transient (`KustoException.IsPermanent == false`) | Anything after **your** `CancellationToken` was cancelled |

Sign-in failures are always retried because a temporary one cannot be told apart from a misconfigured credential, so a wrong credential still takes ~20 seconds to fail.

So a broken query fails at once instead of after ~20 seconds of retries. Commands, streaming queries and ingestion are not retried by Atc.Kusto (the ingestion SDK retries on its own).

## Cancellation

Atc.Kusto supports cooperative cancellation via CancellationToken for all query types. In addition to local cancellation, the library can also issue a server-side cancel control command so the running Kusto query is aborted in the cluster.

### What happens when you cancel

When **your** `CancellationToken` is cancelled, every `IKustoProcessor` method ends with an `OperationCanceledException`, never with `null` or an error log:

- The Kusto SDK often reports a cancelled request with its own exceptions, such as `KustoClientRequestCanceledByUserException`, or with a service error once the server-side cancel has stopped the query. These are translated into an `OperationCanceledException` that carries your token and keeps the SDK's exception as `InnerException`.
- An `OperationCanceledException` that was already thrown is rethrown unchanged.
- Hosts like ASP.NET Core recognise `OperationCanceledException` as cancellation, so a client that disconnects mid-query no longer produces an error log or a 500 response.

This applies to queries, paged queries, streaming and buffered streaming queries, and commands. For a streaming query it also covers the token you pass through `.WithCancellation(token)`, which is how ASP.NET Core passes `RequestAborted` when an endpoint returns the stream.

The SDK's exceptions are only translated when your token was cancelled. A `KustoClientRequestCanceledByUserException` raised for another reason, for example someone running `.cancel query` against your request, is handled like any other failure.

**Commands** (`ExecuteCommand`, and creating the first page of a paged query) cannot be cancelled once they are sent: the Kusto SDK has no cancellation for control commands. If your token is already cancelled, the command is not sent; if it is cancelled while the command runs, the command completes and its outcome is reported as usual. So the result you see always matches what happened on the cluster.

The built-in health check follows the same rule: when its token is cancelled it throws, so the health check service reports a timeout as a timeout and stops quietly on shutdown, instead of reporting the cluster as unhealthy.

### Configuration

Server-side cancel is enabled by default and can be toggled per-call via options:
- For non-streaming queries: `AtcQueryOptions.EnableServerSideCancellation`
- For streaming queries: `AtcStreamingQueryOptions.EnableServerSideCancellation`

### Performance Implications

**Server-side cancellation (default - recommended):**

- ✅ The Kusto cluster immediately stops processing the query upon cancellation
- ✅ Frees up cluster resources (CPU, memory, network) for other queries
- ✅ Reduces unnecessary cluster costs for pay-per-use scenarios
- ⚠️ Adds a small overhead of one additional network call to send the cancel command

**Local-only cancellation (opt-out):**

- ✅ No additional network overhead
- ✅ Slightly faster client-side cancellation response
- ❌ The Kusto cluster continues processing the query even after client cancellation
- ❌ Wastes cluster resources until the query naturally completes or times out
- ❌ May impact cluster performance and increase costs unnecessarily

**When to use each mode:**

- Use **server-side cancellation** (default) for:
  - Long-running queries (> 1 second)
  - Resource-intensive queries
  - Production environments where cluster efficiency matters
  - Scenarios where you pay for cluster usage

- Consider **local-only cancellation** for:
  - Very short queries (< 100ms) where the overhead might exceed query time
  - Testing scenarios where cluster resource usage is not a concern
  - Situations where network reliability to the cluster is poor

### Examples

Direct streaming with cancellation enabled (default):

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
await foreach (var row in processor.ExecuteStreamingQuery(new CustomersStreamingQuery(), cts.Token))
{
        // consume rows
}
```

Opt out of server-side cancellation:

```csharp
var options = new AtcStreamingQueryOptions { EnableServerSideCancellation = false };
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
await foreach (var row in processor.ExecuteStreamingQuery(new CustomersStreamingQuery(), options, cts.Token))
{
        // consume rows
}
```

The API sample exposes endpoints to demonstrate cancellation behavior:

- GET /customers/stream-cancel-demo
- GET /customers/stream-cancel-demo-no-server

## Query Options

Query options can be configured per-call via `AtcQueryOptions` (for standard queries) or `AtcStreamingQueryOptions` (for streaming queries).

### Query Timeout

You can set a server-side query timeout using the `QueryTimeout` property. When set, this limits how long the Kusto cluster will execute your query before timing out:

```csharp
var options = new AtcQueryOptions
{
    QueryTimeout = TimeSpan.FromMinutes(10) // Extend timeout for long-running queries
};

var result = await processor.ExecuteQuery(
    new LongRunningQuery(),
    options,
    cancellationToken);
```

For streaming queries:

```csharp
var options = new AtcStreamingQueryOptions
{
    QueryTimeout = TimeSpan.FromMinutes(10)
};

await foreach (var row in processor.ExecuteStreamingQuery(new LargeDataQuery(), options, cancellationToken))
{
    // Process rows
}
```

**Default behavior:** When `QueryTimeout` is not set (null), the Kusto server default timeout of approximately 4 minutes applies.

## Telemetry

Atc.Kusto supports opt-in OpenTelemetry tracing. When enabled, the library emits spans for query and command execution, which can be collected by any OpenTelemetry-compatible backend.

### Enabling Telemetry

To enable tracing, add the Atc.Kusto activity source to your OpenTelemetry configuration:

```csharp
using Atc.Kusto.Diagnostics;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(KustoDiagnostics.SourceName));
```

### Activity Spans

When enabled, you'll see spans for:
- `kusto.query` - Standard query execution
- `kusto.command` - Command execution
- `kusto.streaming` - Streaming query execution

### Span Attributes

Each span includes:
- `db.statement`: The query or command text

### Zero Overhead When Disabled

If you don't add `KustoDiagnostics.SourceName` to your OpenTelemetry configuration, no activities are created and there is zero performance overhead.

## Analyzer

A Roslyn analyzer is **bundled with the `Atc.Kusto` NuGet package** and provides compile-time validation of your Kusto queries. No separate package installation is required - the analyzer activates automatically when you reference `Atc.Kusto`.

The analyzer validates:

- `.kusto` files exist and are properly configured
- Query parameters match between C# and Kusto
- Projection fields match the result contract type

### Analyzer Rules

| Rule | Severity | Description |
|------|----------|-------------|
| [ATCK301](./docs/rules/ATCK301.md) | Error | Missing `.kusto` embedded resource file |
| [ATCK302](./docs/rules/ATCK302.md) | Error | Parameter count mismatch between C# and Kusto |
| [ATCK303](./docs/rules/ATCK303.md) | Error | Parameter type mismatch between C# and Kusto |
| [ATCK304](./docs/rules/ATCK304.md) | Error | Parameter order mismatch between C# and Kusto |
| [ATCK305](./docs/rules/ATCK305.md) | Warning | Empty `.kusto` script file |
| [ATCK306](./docs/rules/ATCK306.md) | Error | Projection field not found in result type |
| [ATCK307](./docs/rules/ATCK307.md) | Info | Result type property not projected |
| [ATCK308](./docs/rules/ATCK308.md) | Info | Missing final `\| project` statement |
| [ATCK309](./docs/rules/ATCK309.md) | Warning | Projection field naming mismatch (snake_case vs PascalCase) |

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

## How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
