# Atc.Kusto

Atc.Kusto is a .NET library designed to facilitate the execution of Kusto queries and commands within Azure Data Explorer environments/clusters.

The library provides a streamlined interface for handling Kusto operations, making it easier to retrieve and process data efficiently.

> Upgrading from 3.x? See [Upgrading to 4.0](#upgrading-to-40).

## Table of contents

- [Features](#features)
- [Getting started](#getting-started)
  - [Setup with Explicit Parameters](#setup-with-explicit-parameters)
  - [Setup with Pre-Configured Options](#setup-with-pre-configured-options)
  - [Setup with Configuration Delegate](#setup-with-configuration-delegate)
  - [Setup with a Connection String](#setup-with-a-connection-string)
  - [JSON serializer settings](#json-serializer-settings)
- [Queries](#queries)
  - [Adding a Kusto query](#adding-a-kusto-query)
    - [Required Project Configuration](#required-project-configuration)
    - [Defining Query Parameters](#defining-query-parameters)
    - [Reading the result yourself](#reading-the-result-yourself)
  - [Kusto query examples](#kusto-query-examples)
    - [By id](#by-id)
    - [List](#list)
    - [Complex with multiple result sets](#complex-with-multiple-result-sets)
  - [Executing a Kusto query](#executing-a-kusto-query)
    - [When a query fails](#when-a-query-fails)
    - [Paged queries](#paged-queries)
    - [Choosing the connection and database per call](#choosing-the-connection-and-database-per-call)
- [Streaming queries](#streaming-queries)
  - [Direct streaming](#direct-streaming)
  - [Buffered streaming](#buffered-streaming)
  - [When a streaming query fails](#when-a-streaming-query-fails)
  - [Custom row mapping](#custom-row-mapping)
  - [Progressive mode](#progressive-mode)
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
- [Health checks](#health-checks)
  - [Setup Health Check](#setup-health-check)
  - [Health Check Statuses](#health-check-statuses)
  - [Health Check Response](#health-check-response)
  - [Using Health Check Programmatically](#using-health-check-programmatically)
- [Retries](#retries)
- [Cancellation](#cancellation)
  - [What happens when you cancel](#what-happens-when-you-cancel)
  - [Server-side cancellation setting](#server-side-cancellation-setting)
  - [Performance Implications](#performance-implications)
  - [Examples](#examples)
- [Per-call query options](#per-call-query-options)
  - [Query Timeout](#query-timeout)
- [Telemetry](#telemetry)
  - [Enabling Telemetry](#enabling-telemetry)
  - [Activity Spans](#activity-spans)
  - [Span Attributes](#span-attributes)
  - [Zero Overhead When Disabled](#zero-overhead-when-disabled)
- [Upgrading to 4.0](#upgrading-to-40)
  - [Changes that need code changes](#changes-that-need-code-changes)
  - [Changes in behaviour](#changes-in-behaviour)
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
- [Analyzer](#analyzer)
  - [Analyzer Rules](#analyzer-rules)
- [Samples](#samples)
- [Requirements](#requirements)
- [How to contribute](#how-to-contribute)

## Features

The library builds on the official Kusto .NET SDK and adds:

- **Queries and commands as code**: each query is a `.kusto` file embedded in your assembly, paired with a C# record whose properties are the query parameters. Results are mapped to your types.
- **Compile-time checks**: a bundled [analyzer](#analyzer) checks that the `.kusto` file exists, and that its parameters and columns match the record and the result type.
- **Paged queries**: results are stored on the cluster as a stored query result and read page by page with a continuation token, optionally with the total count.
- **Streaming queries**: direct streaming hands out each row as it is read; buffered streaming adds the header, schemas and completion information.
- **Data ingestion**: `IKustoIngestor` ingests in-memory rows, streams and blobs, with managed streaming (default), streaming or queued ingestion, built on the Kusto Ingest V2 SDK.
- **Retries and cancellation**: temporary errors are retried; cancelling your token also cancels the query on the cluster.
- **Several connections**: named connections and per-call databases, each connection with its own JSON serializer settings.
- **Kusto types**: `decimal` (rounded when it has more digits than .NET's `decimal` holds), `bool` (also `0`/`1`), `DateOnly` and `dynamic` columns map to .NET types.
- **Health checks and telemetry**: a cluster health check for ASP.NET Core, and OpenTelemetry tracing.
- **CLI Tool**: Interactive Kusto CLI with query execution, schema export, cluster management, database/table browsing, and multiple output formats.

## Getting started

To seamlessly integrate Azure Data Explorer (Kusto) services into your application, you can utilize the provided `ServiceCollection` extension methods. These methods simplify the setup process and ensure that the Kusto services are correctly configured and ready to use within your application's service architecture.

The extension methods allow you to configure Kusto services using different approaches — explicit parameters, a pre-configured `AtcKustoOptions` instance, or an `Action<AtcKustoOptions>` delegate for dynamic configuration.

All methods ensure that the Kusto services are added to the application's service collection and configured according to the specified parameters, making them available throughout your application via dependency injection.

### Setup with Explicit Parameters

If you prefer to configure Kusto services with explicit values for the cluster's host address, database name, and credentials, you can use the following approach:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureAzureDataExplorer(
    new Uri(builder.Configuration["Kusto:HostAddress"]!),
    builder.Configuration["Kusto:DatabaseName"]!,
    new DefaultAzureCredential());
```

### Setup with Pre-Configured Options

When you already have a pre-configured `AtcKustoOptions` instance, you can directly pass it to the configuration method:

```csharp
var builder = WebApplication.CreateBuilder(args);

var kustoOptions = new AtcKustoOptions
{
    HostAddress = new Uri(builder.Configuration["Kusto:HostAddress"]!),
    DatabaseName = builder.Configuration["Kusto:DatabaseName"],
    Credential = new DefaultAzureCredential(),
};

builder.Services.ConfigureAzureDataExplorer(kustoOptions);
```

### Setup with Configuration Delegate

For more flexibility, you can configure Kusto services using an `Action<AtcKustoOptions>` delegate. This is particularly useful when you need to dynamically adjust settings during application startup:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureAzureDataExplorer(options =>
{
    options.HostAddress = new Uri(builder.Configuration["Kusto:HostAddress"]!);
    options.DatabaseName = builder.Configuration["Kusto:DatabaseName"];
    options.Credential = new DefaultAzureCredential();
});
```

### Setup with a Connection String

Instead of `HostAddress`, a connection can use a Kusto connection string:

```csharp
builder.Services.ConfigureAzureDataExplorer(options =>
{
    options.ConnectionString = "Data Source=https://mycluster.westeurope.kusto.windows.net;Initial Catalog=MyDatabase";
    options.Credential = new DefaultAzureCredential();
});
```

- When `Credential` is set, it is used to sign in; otherwise the connection string must contain the authentication itself.
- `DatabaseName` (or a database passed to `IKustoProcessorFactory.Create`) overrides `Initial Catalog`.
- When `HostAddress` is set too, `HostAddress` is used whenever a database is known.
- A connection string connection can query, but not ingest: [ingestion](#ingesting-data) needs `HostAddress` and `Credential`.

### JSON serializer settings

Each connection has its own JSON serializer settings, `AtcKustoOptions.SerializerOptions`. They are used to map query results and streaming rows to your types, and to write rows for ingestion. The defaults:

- write property names in camelCase (`SerialNumber` → `"serialNumber"`), and read them case-insensitively;
- read numbers that arrive as strings;
- write and read enums as strings;
- handle Kusto's `bool` (also `0`/`1`) and `DateOnly` values.

To change them, call `ConfigureSerializerOptions` when you register the connection. It changes a copy of the defaults, so everything you don't change stays as it is:

```csharp
builder.Services.ConfigureAzureDataExplorer(options =>
{
    options.HostAddress = new Uri(builder.Configuration["Kusto:HostAddress"]!);
    options.DatabaseName = builder.Configuration["Kusto:DatabaseName"];
    options.Credential = new DefaultAzureCredential();
    options.ConfigureSerializerOptions(json => json.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
}, "Sales");
```

Configure them during registration: System.Text.Json doesn't allow changes once the settings have been used by a query or an ingestion. Named connections can have different settings.

## Queries

### Adding a Kusto query

A Kusto query can be added by creating two files in your project:

- A `.kusto` script file containing the Kusto query itself (with "Build Action" set to "Embedded resource")
- A .NET record with the same name (and namespace) as the embedded `.kusto` script.

The .NET record derives from one of the following base types:

| Base type                | Description                                                                                       |
| ------------------------ | ------------------------------------------------------------------------------------------------- |
| `KustoCommand`           | A Kusto command that doesn't produce an output.                                                   |
| `KustoQuery<T>`          | A Kusto query whose rows are returned as an array, `T[]`.                                         |
| `KustoStreamingQuery<T>` | A Kusto query whose rows are returned one at a time (see [streaming](#streaming-queries)). |

> Note: The base types handle the loading of the embedded `.kusto` script file, the passing of parameters and the deserialization of the output.

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

The record's properties are the query's parameters. Declare them at the top of the `.kusto` script, with the property names in camelCase:

```csharp
// file: GetTeamQuery.cs
public record GetTeamQuery(long TeamId)
    : KustoQuery<Team>;
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

Each row of the result is mapped to the result type, by matching the column names to its property names (case-insensitive):

```csharp
// file: Team.cs
public record Team(
    long Id,
    string Name);
```

`KustoQuery<Team>` returns a `Team[]`, which is empty when no row matches.

A property whose value is `null` is not sent, so the default value in `declare query_parameters` applies. That makes a parameter optional:

```kusto
declare query_parameters (
    customerId:long = long(null))
;
Customers
| where isnull(customerId) or CustomerKey == customerId
```

#### Reading the result yourself

To return something other than an array, implement `IKustoQuery<T>` and its `ReadResult` method yourself. For example, to return a single team, or `null` when there is none:

```csharp
// file: GetTeamQuery.cs
public record GetTeamQuery(long TeamId)
    : KustoScript, IKustoQuery<Team>
{
    public Team? ReadResult(
        IDataReader reader,
        JsonSerializerOptions serializerOptions)
        => reader
            .ReadObjects<Team>(serializerOptions)
            .FirstOrDefault();
}
```

`ReadResult` receives the connection's [JSON serializer settings](#json-serializer-settings). Pass them on to `ReadObjects`, `ReadObject` and `ReadObjectsFromNextResult`, which require them.

Often it's simpler to keep `KustoQuery<Team>` and pick the row where the query is executed: `(await processor.ExecuteQuery(new GetTeamQuery(teamId), cancellationToken))?.FirstOrDefault()`. A `KustoQuery<T>` is also checked by the [analyzer](#analyzer) against its result type, which a custom `IKustoQuery<T>` is not.

### Kusto query examples

The following examples demonstrate different types of queries: a query by id, a list query, and a more complex query with multiple result sets.

#### By id

`ExecuteQuery` returns an array with the matching customer, or an empty array.

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
    SalesAmount = todecimal(round(sum(SalesAmount), 2)),
    TotalCost = todecimal(round(sum(TotalCost), 2))
  by CustomerKey, CustomerName
```

#### Complex with multiple result sets

> The following C# record is defined in the [CustomersSplitByGenderQuery.cs](./sample/Atc.Kusto.Sample/Queries/CustomersSplitByGenderQuery.cs) file:

```csharp
public record CustomersSplitByGenderQuery
    : KustoScript, IKustoQuery<CustomersByGender>
{
    public CustomersByGender ReadResult(
        IDataReader reader,
        JsonSerializerOptions serializerOptions)
        => new(
            reader.ReadObjects<Customer>(serializerOptions),
            reader.ReadObjectsFromNextResult<Customer>(serializerOptions),
            reader.ReadObjectsFromNextResult<CustomerGenderCount>(serializerOptions));
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

#### When a query fails

Temporary errors, such as throttling or a dropped connection, are retried first (see [Retries](#retries)). When the query still fails, or fails with an error that retrying can't fix, such as a misspelled column, `ExecuteQuery` throws the exception as it is. So a failing query can never be mistaken for "no data".

Most failures are a `KustoException` from the Kusto SDK, for example a `SemanticException` for a misspelled column, or a `KustoServiceException` when the cluster can't be reached. But not all of them:

- a query that is cancelled on the cluster (`.cancel query`) fails with the SDK's `UtilsException`, which is not a `KustoException`;
- a sign-in failure is an `AuthenticationFailedException` from Azure.Identity;
- a result that can't be mapped to your type is a `JsonException`.

`ExecuteQuery` returns `null` only when your own `ReadResult` returns `null`, for example a `FirstOrDefault()` that finds no rows. A `KustoQuery<T>` returns an array, which is empty when there are no rows.

In an ASP.NET Core endpoint, an exception you don't catch becomes a 500 response. Catch it where a failure needs other handling, for example in a background job that should keep its last known state and try again later. Catch `Exception`, not only `KustoException`, and let cancellation through:

```csharp
try
{
    var alarms = await processor.ExecuteQuery(
        new ActiveAlarmsQuery(),
        cancellationToken);

    // ...
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    logger.LogWarning(ex, "Could not read the active alarms, keeping the current state");
}
```

#### Paged queries

`ExecutePagedQuery` returns one page at a time. The first call runs the query once and saves the whole result in the cluster as a *stored query result*, then returns the first page and a `ContinuationToken`. Pass the token back to get the next page: it is read from the saved result, without running the query again. When `ContinuationToken` is `null`, there are no more pages.

A full page always gets a token, because there may be more items. So when the number of items is an exact multiple of the page size, the last call returns an empty page without a token. With the [total count](#total-count), paging stops at the last full page instead.

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

- `pageSize` is the number of items per page, and must be at least 1 (otherwise `ArgumentOutOfRangeException`). `null` runs the query once and returns every item in one page.
- `sessionId` is optional. Without it, every call for page 1 saves a new stored result, so two users never share one. With it, starting again from page 1 with the same query and parameter values replaces that session's stored result instead of adding another one.

##### Continuation tokens

A token looks like `v2;{nonce};{itemsReturned}`, followed by `;{totalCount}` when the [total count](#total-count) was requested. The nonce is a random value created on page 1 when there is no session id.

The token doesn't contain the name of the stored result. The name is calculated on the server, on every page, from the query type, the query text, the parameter values, the session id and the nonce. So:

- a token only reads the stored result of the exact query it was created for. Used with other parameter values (for example another customer), it finds no stored result, so it is handled like an [expired one](#when-the-stored-result-has-expired): the query runs again with the new values, and you get the rows at the token's position in that new result. Never the rows of the first query;
- a token that was edited can't point at another stored result. At most it makes the query run again.

##### When the stored result has expired

The cluster deletes a stored result after `AtcKustoOptions.PagedResultExpiration`: 1 hour by default, and anything from 1 second to 24 hours (whole seconds). A value outside that range makes every paged query on that connection throw `InvalidOperationException`. A page requested after that runs the query again, saves the new result under the same name, and returns the requested page.

The new run uses the data as it is now, so rows that were added or removed in the meantime can shift between pages: a page can repeat rows or skip some. **Sort a paged query with an `order by` that includes a unique column**, such as `| order by Name asc, Id asc`, so a new run numbers the rows the same way. The `order by` can come before the final `project`, which keeps the order. Without a sort order, the rows can come back in a completely different order. Sorting is cheap for lists that are paged through, and only page 1 (or a new run) runs the query.

A longer expiration means fewer new runs, while the cluster keeps each result for longer:

```csharp
services.ConfigureAzureDataExplorer(options =>
{
    options.HostAddress = new Uri("https://mycluster.westeurope.kusto.windows.net");
    options.DatabaseName = "MyDatabase";
    options.Credential = new DefaultAzureCredential();
    options.PagedResultExpiration = TimeSpan.FromHours(4);
});
```

##### Total count

To also get the total number of items across all pages, pass `AtcPagedQueryOptions`:

```csharp
var result = await processor.ExecutePagedQuery(
    new CustomersQuery(),
    sessionId,
    pageSize: 100,
    continuationToken,
    new AtcPagedQueryOptions
    {
        IncludeTotalCount = true,
    },
    cancellationToken);

long? total = result?.TotalCount;
```

The total count is opt-in: without it, `TotalCount` is `null` and nothing else changes. When it is requested:

- when page 1 isn't full, its item count is the total;
- when page 1 is full, the total is read once from the stored result's metadata (`.show stored_query_results`), which doesn't run the query again;
- the total travels in the continuation token, so later pages need no extra call, and paging stops as soon as the total is reached;
- after a new run of an expired result, the total is read again.

##### Errors

Temporary errors are retried on every page (see [Retries](#retries)). An error that remains is thrown, on every page, just like [`ExecuteQuery`](#when-a-query-fails). A client can then ask for the same page again with the same continuation token.

`ExecutePagedQuery` returns `null` only for a continuation token it can't read: one that isn't in the format above, such as a token from before 4.0.

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

## Streaming queries

A streaming query returns its rows one at a time, mapped with [`MapRecord`](#custom-row-mapping). Atc.Kusto provides two ways to run one:

| | Direct streaming (`ExecuteStreamingQuery`) | Buffered streaming (`ExecuteBufferedStreamingQuery`) |
| --- | --- | --- |
| First row | As soon as it is read from the response | After the whole result has been read |
| Memory | Atc.Kusto keeps no rows | Every row is held until you have read it |
| Header, schema, completion | No | Yes |
| When the query fails | Throws, possibly after some rows (see [below](#when-a-streaming-query-fails)) | Doesn't throw: `Completion.HasErrors` |

### Direct streaming

Direct streaming maps each row as it is read from the response and hands it to you; Atc.Kusto doesn't keep it. Use it for large results, or when you want the first rows as early as possible:

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

### Buffered streaming

Buffered streaming reads the **whole result** before `ExecuteBufferedStreamingQuery` returns, so every row is in memory at that point; `Rows` then hands them to you one at a time. In return you get the data-set header, the table schemas and the completion information. Use it when you need that information, not to save memory:

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

In a web API, you can return the stream directly. ASP.NET Core writes it as a JSON array while the rows arrive:

```csharp
app.MapGet(
    "/customers/stream",
    (IKustoProcessor processor, CancellationToken cancellationToken)
        => processor.ExecuteStreamingQuery(
            new CustomersStreamingQuery(),
            cancellationToken))
    .WithName("GetCustomersStream");
```

Once the first rows are sent, the status code can't change anymore: when the query fails halfway, the response is aborted instead of becoming a 500.

### When a streaming query fails

**Direct streaming** throws the exception from the Kusto SDK, like [`ExecuteQuery`](#when-a-query-fails). A failure can come after rows were already handed out, so code that writes rows somewhere as they arrive must be ready for a partial result.

**Buffered streaming** doesn't throw for a failing query (only for [cancellation](#cancellation)). It returns a result with `Completion.HasErrors = true` and the error in `Completion.ErrorMessage`, and `Rows` is empty (or, when mapping a row failed, holds the rows mapped before it). Always check `Completion`:

```csharp
var result = await kustoProcessor.ExecuteBufferedStreamingQuery(
    new CustomersStreamingQuery(),
    cancellationToken);

if (result?.Completion?.HasErrors == true)
{
    logger.LogError("The query failed: {Error}", result.Completion.ErrorMessage);
    return;
}
```

A common failure for large results is Kusto's result limit: by default a query may return at most 500,000 rows or 64 MB. For a 600,000-row result, direct streaming hands out the first 500,000 rows and then throws `KustoServicePartialQueryFailureLimitsExceededException`; buffered streaming returns no rows and `HasErrors = true`. To read a larger result, set `NoTruncation` (or raise `TruncationMaxRecords`/`TruncationMaxSize`) in the [per-call query options](#per-call-query-options).

### Custom row mapping

By default each row is mapped the same way as a `KustoQuery<T>` result (columns matched to properties by name, case-insensitive), with the connection's [JSON serializer settings](#json-serializer-settings). To map a row yourself, override `MapRecord`. Read the values you need inside the method: the `IDataRecord` is only valid during the call. Return `null` to skip a row.

```csharp
public record CustomerNamesStreamingQuery()
    : KustoStreamingQuery<string>
{
    public override string? MapRecord(
        IDataRecord record,
        JsonSerializerOptions serializerOptions)
        => $"{record.GetString(record.GetOrdinal("FirstName"))} {record.GetString(record.GetOrdinal("LastName"))}";
}
```

### Progressive mode

`AtcStreamingQueryOptions.ProgressiveEnabled` controls how Kusto packages the result. It is **off by default**.

- **Off**: Kusto sends each result table as one package, and Atc.Kusto reads it row by row as it arrives. The rows are always final.
- **On**: Kusto sends the table in chunks and may send a *preview* while the query is still running. A later chunk can **replace** the rows sent so far, for example when a `summarize` sends an early count and then the final one.

What happens when Kusto replaces rows:

| | Progressive off | Progressive on |
|---|---|---|
| **Direct streaming** (`ExecuteStreamingQuery`) | Final rows only | Rows are handed out immediately and can't be taken back: a replace after rows were returned ends the stream with an `InvalidOperationException` |
| **Buffered streaming** (`ExecuteBufferedStreamingQuery`) | Final rows only | The replace is applied before any row is handed out, so you get the final rows |

When to turn it on:

- Only for **long-running queries** where seeing a preview early is worth it, preferably with buffered streaming.
- Be careful with **aggregations** (`summarize`, `count`, `dcount`, `top` over large data) in direct streaming: they are the queries Kusto replaces.
- Plain `take`/`where`/`project` queries gain nothing: their rows arrive just as fast (often faster) with progressive mode off.

```csharp
var result = await kustoProcessor.ExecuteBufferedStreamingQuery(
    new SalesSummaryQuery(),
    new AtcStreamingQueryOptions { ProgressiveEnabled = true },
    cancellationToken);
```

## Ingesting data

`IKustoIngestor` writes data into a Kusto table. It is registered automatically by `ConfigureAzureDataExplorer(...)` — there is no separate opt-in — and supports three sources: in-memory rows, a `Stream`, and a blob.

> **Requirements**
>
> - The connection must be configured with **both `HostAddress` and a `Credential`**. A `ConnectionString`-only or credential-less connection can query but not ingest; the first ingest call throws `InvalidOperationException`.
> - The target table must exist. For JSON payloads it also needs a **JSON ingestion mapping**, referenced by name.
> - Real streaming needs streaming ingestion enabled on the cluster and a streaming ingestion policy on the table or database (`.alter table MyTable policy streamingingestion enable`).

### Ingestion quick start

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

### Checking the result

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
| `ArgumentNullException`       | `rows`, `data`, `blobUri` or `target` is `null`.                                                        |
| `ArgumentException`           | Invalid request: missing table name, JSON format without `MappingReference`, in-memory rows with a format other than `Json`/`MultiJson`, `EnableTracking` with `Streaming`, no resolvable database, a non-seekable stream, a relative blob URI, more than 10 MB for `Streaming`. The target is checked before any data is read; the 10 MB limit for in-memory rows is checked once they are serialized. |
| `InvalidOperationException`   | The connection has no `HostAddress` or `Credential`.                                                    |
| `OperationCanceledException`  | Your `CancellationToken` was cancelled. The original exception, if any, is the `InnerException`.         |

`KustoIngestionResult` also carries `Mode` (the resolved mode — the target's `Mode` or the connection default; after a managed fallback it is still `ManagedStreaming`, while `Status` says `Queued`), `OperationId` (the service's id for the operation — useful for correlating with cluster logs) and `OperationHandle` (see [Tracking](#tracking-queued-ingestion)).

### Ingestion modes

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

### JSON property names and ingestion mappings

In-memory rows are serialized to multijson with **camelCase property names**: `SerialNumber` becomes `"serialNumber"`. The table's JSON mapping paths are **case-sensitive** and must match:

```kusto
'[{"column":"serialNumber","path":"$.serialNumber"}]'   // ✅ matches
'[{"column":"serialNumber","path":"$.SerialNumber"}]'   // ❌ ingests successfully, but the column is left empty
```

A mismatch does **not** fail the ingestion — the rows arrive with empty columns. To use other names, annotate properties with `[JsonPropertyName("...")]`, change the connection's [JSON serializer settings](#json-serializer-settings), or pass `serializerOptions` to `IngestAsync` for a single call (note: options passed to a call replace the connection's settings entirely, including enum-as-string and the Kusto `bool`/`DateOnly` handling).

Rename a single property — everything else stays camelCase:

```csharp
public sealed record DeviceReading(
    [property: JsonPropertyName("device_id")] string DeviceId,   // written as "device_id"
    string SerialNumber,                                         // written as "serialNumber"
    double Value,
    DateTimeOffset Timestamp);
```

Change the naming for every ingestion on a connection, e.g. when its tables' mappings use snake_case paths (`$.serial_number`):

```csharp
builder.Services.ConfigureAzureDataExplorer(options =>
{
    // ...
    options.ConfigureSerializerOptions(json => json.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
});
```

Or for a single call. Options passed to a call replace the connection's settings, so start from a copy of them to keep everything else (enums as strings, the Kusto `bool`/`DateOnly` handling):

```csharp
public sealed class ReadingsWriter(
    IKustoIngestor ingestor,
    IOptionsMonitor<AtcKustoOptions> kustoOptions)
{
    // A copy of the default connection's settings, with snake_case names: SerialNumber → "serial_number".
    private readonly JsonSerializerOptions snakeCase = new(kustoOptions.CurrentValue.SerializerOptions)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public Task<KustoIngestionResult> WriteAsync(
        IReadOnlyList<DeviceReading> readings,
        KustoIngestTarget target,
        CancellationToken cancellationToken)
        => ingestor.IngestAsync(
            readings,
            target,
            serializerOptions: snakeCase,
            cancellationToken: cancellationToken);
}
```

For a named connection, copy `kustoOptions.Get("name").SerializerOptions` instead.

### Ingesting a stream or a blob

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

### `KustoIngestTarget`

| Property           | Required | Description                                                                                     |
| ------------------ | -------- | ----------------------------------------------------------------------------------------------- |
| `TableName`        | yes      | Destination table.                                                                              |
| `Format`           | yes      | `MultiJson`, `Json`, `Csv` or `Tsv`. In-memory rows require `MultiJson` or `Json`.              |
| `MappingReference` | JSON     | Name of the table's ingestion mapping. Required for `Json`/`MultiJson`, optional for CSV/TSV.   |
| `Mode`             | no       | Overrides `AtcKustoOptions.DefaultIngestionMode` (default `ManagedStreaming`).                  |
| `ConnectionName`   | no       | Named connection; `null` uses the default connection.                                           |
| `DatabaseName`     | no       | Overrides the connection's `DatabaseName`.                                                      |
| `EnableTracking`   | no       | Request an operation handle (queued/managed only; rejected for pure `Streaming`).               |

### Tracking queued ingestion

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

Unlike the ingest methods, a failure to **check** the status (cluster unreachable, credential failure) throws `KustoIngestionException` — the check only reads, so it is safe to retry. An empty or unreadable handle throws `ArgumentException`. An ingestion that was streamed (managed streaming without fallback) reports `Succeeded` immediately without contacting the cluster.

### Retries and duplicates

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

### Private endpoints: upload containers

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

### Dependencies

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

## Health checks

The library provides built-in health check support for Azure Data Explorer clusters, which can be easily integrated with ASP.NET Core's Health Checks API. This feature allows you to monitor the health of your Kusto clusters and integrate it with your application's monitoring infrastructure.

### Setup Health Check

Add the check with `AddKustoClusterDiagnosticsHealthCheck`. Every parameter is optional:

```csharp
builder.Services
    .AddHealthChecks()
    .AddKustoClusterDiagnosticsHealthCheck(
        name: "adx",                              // default "kusto"
        connectionName: null,                     // a named connection, or null for the default one
        databaseName: null,                       // null uses the connection's DatabaseName
        failureStatus: HealthStatus.Unhealthy,    // reported when the cluster is not healthy (default Unhealthy)
        tags: ["adx", "database"],
        timeout: TimeSpan.FromSeconds(30));
```

The check runs `.show diagnostics` on the cluster. It's an ordinary query, so it's [retried](#retries) like any other: when the cluster can't be reached, the check reports after about 20 seconds of retries. Set `timeout` to report sooner.

### Health Check Statuses

| Cluster diagnostics | Health status |
| --- | --- |
| Healthy | `Healthy` |
| Healthy, but attention required | `Degraded` |
| Not healthy | `failureStatus` (default `Unhealthy`) |
| The check fails, e.g. the cluster can't be reached | `failureStatus`, with the error message and the exception |
| Healthy, but scale-out recommended | `Healthy` (see `isScaleOutRequired` below) |

When the health check service cancels the check (its `timeout`, or shutdown), the check throws `OperationCanceledException`, so it's reported as a timeout instead of an unhealthy cluster.

### Health Check Response

The health check result's `Data` contains:

| Key | Value |
| --- | --- |
| `duration` | How long the check took. |
| `isAttentionRequired` | Whether the cluster requires attention. |
| `isScaleOutRequired` | Whether scaling out the cluster is recommended. |
| `notHealthyReason` | Why the cluster isn't healthy (only when there is a reason). |
| `attentionRequiredReason` | Why attention is required (only when there is a reason). |

### Using Health Check Programmatically

`IKustoClusterDiagnosticsHealthCheck` is registered by `ConfigureAzureDataExplorer(...)`, so you can also check a cluster from your own code:

```csharp
public sealed class ClusterStatusService(
    IKustoClusterDiagnosticsHealthCheck healthCheck)
{
    public async Task<bool> IsClusterHealthyAsync(
        CancellationToken cancellationToken)
    {
        var result = await healthCheck.CheckHealthAsync(
            connectionName: "Reporting",
            cancellationToken: cancellationToken);

        if (!result.IsHealthy)
        {
            Console.WriteLine($"Cluster unhealthy: {result.NotHealthyReason}");
        }

        return result.IsHealthy;
    }
}
```

`CheckHealthAsync` doesn't throw when the cluster is unhealthy or can't be reached: it returns a result with `IsHealthy = false` and the reason. It only throws `OperationCanceledException` when your token is cancelled.

For more details, check the [health check sample](./sample/Atc.Kusto.HealthCheck.Sample/).

## Retries

Queries (`ExecuteQuery`, and every page of `ExecutePagedQuery`) are retried automatically: up to **3 retries**, waiting 3, 6 and 12 seconds. Each retry is logged as a warning. Page 1 of a paged query is created with a control command, which is safe to retry because it replaces the stored result.

Only failures that can succeed on a later attempt are retried:

| Retried | Not retried |
|---|---|
| Throttling, service timeouts, service unavailable, connection and network failures | Syntax and semantic errors (e.g. a misspelled column), database or table not found, access denied by the cluster |
| Sign-in failures (getting a token), e.g. a managed identity that is not ready yet right after a deploy | Errors in your own code, such as a result that cannot be mapped to your type |
| Any other Kusto error the SDK marks as transient (`KustoException.IsPermanent == false`) | Anything after **your** `CancellationToken` was cancelled |

So a broken query fails at once instead of after ~20 seconds of retries.

Sign-in failures are always retried because a temporary one cannot be told apart from a misconfigured credential, so a wrong credential still takes ~20 seconds to fail.

When the retries don't help, the last error is thrown (see [When a query fails](#when-a-query-fails)). Commands, streaming queries and ingestion are not retried by Atc.Kusto (the ingestion SDK retries on its own).

## Cancellation

Atc.Kusto supports cooperative cancellation via CancellationToken for all query types. In addition to local cancellation, the library can also issue a server-side cancel control command so the running Kusto query is aborted in the cluster.

### What happens when you cancel

When **your** `CancellationToken` is cancelled, the query methods of `IKustoProcessor` end with an `OperationCanceledException`, never with `null`, an error result or an error log (commands are the exception, see below):

- The Kusto SDK often reports a cancelled request with its own exceptions, such as `KustoClientRequestCanceledByUserException`, or with a service error once the server-side cancel has stopped the query. These are translated into an `OperationCanceledException` that carries your token and keeps the SDK's exception as `InnerException`.
- An `OperationCanceledException` that was already thrown is rethrown unchanged. That's also the case when your token was not cancelled, for example when the SDK reports an HTTP timeout as a `TaskCanceledException`.
- Hosts like ASP.NET Core recognise `OperationCanceledException` as cancellation, so a client that disconnects mid-query doesn't produce an error log or a 500 response.

This applies to queries, paged queries, streaming and buffered streaming queries, and commands. For a streaming query it also covers the token you pass through `.WithCancellation(token)`, which is how ASP.NET Core passes `RequestAborted` when an endpoint returns the stream.

The SDK's exceptions are only translated when your token was cancelled. A `KustoClientRequestCanceledByUserException` raised for another reason, for example someone running `.cancel query` against your request, is handled like any other failure.

**Commands** (`ExecuteCommand`, and creating the first page of a paged query) cannot be cancelled once they are sent: the Kusto SDK has no cancellation for control commands. If your token is already cancelled, the command is not sent; if it is cancelled while the command runs, the command completes and its outcome is reported as usual. So the result you see always matches what happened on the cluster.

The built-in health check follows the same rule: when its token is cancelled it throws, so the health check service reports a timeout as a timeout and stops quietly on shutdown, instead of reporting the cluster as unhealthy.

### Server-side cancellation setting

Server-side cancel is enabled by default and can be turned off per call:

- for queries: `AtcQueryOptions.EnableServerSideCancellation`;
- for streaming queries: `AtcStreamingQueryOptions.EnableServerSideCancellation`.

Paged queries always use it for page 2 and later; page 1 is a command, which can't be cancelled once it is sent.

### Performance Implications

Server-side cancel costs nothing until your token is cancelled: Atc.Kusto only registers a callback on the token. When the token is cancelled, it sends one `.cancel query` command in the background, which doesn't delay the `OperationCanceledException` you get.

- **On (default):** the cluster stops the query, which frees its CPU and memory for other queries.
- **Off:** the cluster keeps running the query until it completes or times out, although nobody reads the result.

When the cancel command fails, that's logged as an error, and you still get the `OperationCanceledException` as usual. There is rarely a reason to turn it off.

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

## Per-call query options

Query options can be set per call with `AtcQueryOptions` (for `ExecuteQuery`) or `AtcStreamingQueryOptions` (for streaming queries). Paged queries don't take these options.

| Option | Kusto request property | What it does |
| --- | --- | --- |
| `QueryTimeout` | `servertimeout` | How long the cluster runs the query before it times out. Default: the cluster's default, about 4 minutes. |
| `QueryTakeMaxRecords` | `query_take_max_records` | Returns at most this many rows. The rest is dropped silently, without an error. |
| `NoTruncation` | `notruncation` | Lifts the result limit of 500,000 rows or 64 MB. |
| `TruncationMaxRecords` | `truncationmaxrecords` | Changes the row limit. |
| `TruncationMaxSize` | `truncationmaxsize` | Changes the size limit, in bytes. |
| `EnableServerSideCancellation` | | Sends `.cancel query` when your token is cancelled (default `true`, see [Cancellation](#server-side-cancellation-setting)). |

`AtcStreamingQueryOptions` also has `ProgressiveEnabled` (see [Progressive mode](#progressive-mode)) and `OptionalFrames` (which frames buffered streaming reads into its result; the default includes all of them).

A query that goes over the result limit fails: see [When a streaming query fails](#when-a-streaming-query-fails) for what that looks like.

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

Atc.Kusto supports opt-in OpenTelemetry tracing. When enabled, the library emits spans for queries, commands, streaming queries and ingestion, which can be collected by any OpenTelemetry-compatible backend.

### Enabling Telemetry

To enable tracing, add the Atc.Kusto activity source to your OpenTelemetry configuration:

```csharp
using Atc.Kusto.Diagnostics;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(KustoDiagnostics.SourceName));
```

### Activity Spans

| Span | Created by |
| --- | --- |
| `kusto.query` | `ExecuteQuery` (also the health check) |
| `kusto.command` | `ExecuteCommand` |
| `kusto.streaming` | `ExecuteStreamingQuery` and `ExecuteBufferedStreamingQuery` |
| `kusto.ingest` | `IngestAsync` and `IngestFromBlobAsync` (not for `Skipped` ingestions, which don't contact the cluster) |

Paged queries (`ExecutePagedQuery`) don't create spans. A span's status is `Error` when the operation failed.

### Span Attributes

| Attribute | Spans | Value |
| --- | --- | --- |
| `db.statement` | `kusto.query`, `kusto.command`, `kusto.streaming` | The query or command text |
| `kusto.ingest.database` | `kusto.ingest` | The target database |
| `kusto.ingest.table` | `kusto.ingest` | The target table |
| `kusto.ingest.mode` | `kusto.ingest` | The resolved ingestion mode |
| `kusto.ingest.status` | `kusto.ingest` | The result's `Status` (`Succeeded`, `Queued` or `Failed`) |
| `kusto.ingest.operation_id` | `kusto.ingest` | The service's operation id, when the ingestion was accepted |

### Zero Overhead When Disabled

If you don't add `KustoDiagnostics.SourceName` to your OpenTelemetry configuration, no activities are created and there is zero performance overhead.

## Upgrading to 4.0

4.0 changes a few extension points, and how failures and cancellation are reported. Go through this list when you upgrade from 3.x.

### Changes that need code changes

**Custom `ReadResult` implementations.** `IKustoQuery<T>.ReadResult` gets the connection's [JSON serializer settings](#json-serializer-settings) as a second parameter, and `ReadObjects<T>` and `ReadObjectsFromNextResult<T>` (like the new `ReadObject<T>`) require them. Queries that inherit `KustoQuery<T>` need no change.

```csharp
// 3.x
public Team? ReadResult(IDataReader reader)
    => reader.ReadObjects<Team>().FirstOrDefault();

// 4.0
public Team? ReadResult(
    IDataReader reader,
    JsonSerializerOptions serializerOptions)
    => reader
        .ReadObjects<Team>(serializerOptions)
        .FirstOrDefault();
```

**Custom row mapping in streaming queries.** `MapDataRow(DataRow row)` is now `MapRecord(IDataRecord record, JsonSerializerOptions serializerOptions)` (see [Custom row mapping](#custom-row-mapping)):

- `row["Name"]` becomes `record["Name"]`;
- `row.Field<T>("Name")` becomes `(T)record["Name"]`, or one of the typed getters, such as `record.GetString(...)`;
- a null value is `DBNull.Value`, so check `record.IsDBNull(...)` first;
- the record is only valid during the call, so read the values you need inside the method.

```csharp
// 3.x
public override string? MapDataRow(DataRow row)
    => $"{row["FirstName"]} {row["LastName"]}";

// 4.0
public override string? MapRecord(
    IDataRecord record,
    JsonSerializerOptions serializerOptions)
    => $"{record["FirstName"]} {record["LastName"]}";
```

**Your own implementations of `IKustoProcessor` or `IScriptHandlerFactory`.** `IKustoProcessor` has a new `ExecutePagedQuery` overload with `AtcPagedQueryOptions`, and the paged `IScriptHandlerFactory.Create` has a new optional `options` parameter. Add them to your implementation. Mocks, for example with NSubstitute, need no change.

### Changes in behaviour

**Failing queries throw.** In 3.x, `ExecuteQuery` and page 2 or later of `ExecutePagedQuery` logged the error and returned `null`, so a failing query looked like a query without rows. In 4.0 they throw the exception once the retries are used up (usually, but not always, a `KustoException`), on every page and also without a page size. Check code that treats `null` as "no data": `null` now only means that your own `ReadResult` returned `null`, or (paged) that the continuation token can't be read. See [When a query fails](#when-a-query-fails).

**Cancellation throws `OperationCanceledException`.** In 3.x, when your token was cancelled, the Kusto SDK's cancel errors were logged and the query returned `null`. In 4.0 queries, paged queries and streaming queries end with an `OperationCanceledException`, with the SDK's error as `InnerException`. See [Cancellation](#cancellation).

**The health check throws when its token is cancelled.** In 3.x, `IKustoClusterDiagnosticsHealthCheck.CheckHealthAsync` and the registered health check reported a cancelled check as an unhealthy cluster. In 4.0 they throw `OperationCanceledException`, so ASP.NET Core reports a timeout as a timeout and stops quietly on shutdown. See [Health checks](#health-checks).

**Progressive mode is off by default.** In 3.x, `AtcStreamingQueryOptions.ProgressiveEnabled` was on, and rows that Kusto replaced were added to the earlier ones, which gave duplicate and partial rows for aggregations. In 4.0 it is off. To turn it on, set `ProgressiveEnabled = true`; see [Progressive mode](#progressive-mode) for how replaced rows are handled.

**Continuation tokens have a new format.** In 3.x the token contained the name of the stored result. In 4.0 it looks like `v2;{nonce};{itemsReturned}`, and the name is calculated on the server (see [Continuation tokens](#continuation-tokens)). `ExecutePagedQuery` returns `null` for a token from 3.x, so a client that is paging during the upgrade starts again from page 1.

**Other changes you may notice:**

- `ExecutePagedQuery` throws `ArgumentOutOfRangeException` for a page size below 1.
- A page requested after the stored result has expired runs the query again, instead of failing. Sort paged queries with an `order by` (see [When the stored result has expired](#when-the-stored-result-has-expired)).
- Page 1 of a paged query is also retried on temporary errors.

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

## Samples

| Sample | Shows |
| --- | --- |
| [Atc.Kusto.Api.Sample](./sample/Atc.Kusto.Api.Sample/) | Configuring the library in a minimal API: queries, paged queries, streaming endpoints and an ingestion endpoint. |
| [Atc.Kusto.Api.Client.Sample](./sample/Atc.Kusto.Api.Client.Sample/) | A client that reads the API sample's streaming endpoints. |
| [Atc.Kusto.Sample](./sample/Atc.Kusto.Sample/) | Using the library directly in a console application, without an API. |
| [Atc.Kusto.Ingestion.Sample](./sample/Atc.Kusto.Ingestion.Sample/) | Every ingestion scenario: modes, tracking, streams, empty input, mapping mismatches, failures. Needs a writable cluster. |
| [Atc.Kusto.HealthCheck.Sample](./sample/Atc.Kusto.HealthCheck.Sample/) | The cluster health check. |
| [Atc.Kusto.Analyzer.Sample](./sample/Atc.Kusto.Analyzer.Sample/) | One example per analyzer rule. |

The query samples use the "ContosoSales" and "Samples" databases of the Microsoft ADX sample cluster (`https://help.kusto.windows.net`).

## Requirements

- .NET 9 or later (the package targets `net9.0`).

## How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
