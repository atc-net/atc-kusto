# Introduction

Atc.Kusto is a .NET library designed to facilitate the execution of Kusto queries and commands within Azure Data Explorer environments/clusters.

The library provides a streamlined interface for handling Kusto operations, making it easier to retrieve and process data efficiently.

# Table of Content

- [Introduction](#introduction)
- [Table of Content](#table-of-content)
  - [Features](#features)
  - [Getting started](#getting-started)
    - [Configuring the Atc.Kusto library using ServiceCollection Extensions](#configuring-the-atckusto-library-using-servicecollection-extensions)
      - [Setup with Explicit Parameters](#setup-with-explicit-parameters)
      - [Setup with Pre-Configured Options](#setup-with-pre-configured-options)
      - [Setup with Configuration Delegate](#setup-with-configuration-delegate)
    - [Adding a Kusto query](#adding-a-kusto-query)
    - [Kusto query examples](#kusto-query-examples)
      - [Single](#single)
      - [List](#list)
      - [Complex with multiple result sets](#complex-with-multiple-result-sets)
    - [Executing a Kusto query](#executing-a-kusto-query)
    - [Executing streaming queries](#executing-streaming-queries)
      - [Direct streaming](#direct-streaming)
      - [Buffered streaming](#buffered-streaming)
    - [Health Checks](#health-checks)
      - [Setup Health Check](#setup-health-check)
      - [Health Check Response](#health-check-response)
      - [Health Check Statuses](#health-check-statuses)
      - [Using Health Check Programmatically](#using-health-check-programmatically)
  - [Sample](#sample)
- [Requirements](#requirements)
- [How to contribute](#how-to-contribute)

## Features

The library extends the official .NET SDK, and adds the following add-on functionality, which supports passing parameters and proper deserialization:

- **Kusto Query and Command Execution**: Simplifies the execution of Kusto queries and commands with asynchronous support through embedded .kusto scripts.
- **Paged Query Support**: Efficient handling of large datasets with built-in support for paginated query results through stored query results.
- **Streaming Query Support**: Two approaches for streaming large result sets:
  - **Direct Streaming**: Immediately yield rows as they become available, minimizing memory usage and latency.
  - **Buffered Streaming**: Buffer results with additional metadata like schemas and completion information.
- **Health Checks**: Built-in health check integration for Azure Data Explorer clusters with ASP.NET Core's Health Checks API.

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

  * A `.kusto` script file containing the Kusto query itself (with "Build Action" set to "Embedded resource")
  * A .NET record with the same name (and namespace) as the embedded `.kusto` script.

The .NET record should to derive from one of the following base types:

| Base type       | Description                                            |
| --------------- | ------------------------------------------------------ |
| `KustoCommand`  | Used for Kusto commands that do not produce an output. |
| `KustoQuery<T>` | Used for Kusto queries that returns a result.          |

> Note: The base types handles the loading of the embedded `.kusto` script file, passing of parameters and deserialization of the output._

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
        Task.FromResult(processorFactory.Create("DatabaseName")
            .ExecuteStreamingQuery(
                new CustomersStreamingQuery(),
                cancellationToken)))
    .WithName("GetCustomersStream");
```

This returns a streamed response to the client, which can be processed as it arrives.

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

# Requirements

* [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

# How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
