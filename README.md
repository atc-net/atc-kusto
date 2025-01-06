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
    - [Executing a Kusto query](#executing-a-kusto-query)
  - [Sample](#sample)
- [Requirements](#requirements)
- [How to contribute](#how-to-contribute)

## Features

The library extends the official .NET SDK, and adds the following add-on functionality, which supports passing parameters and proper deserialization:

- **Kusto Query and Command Execution**: Simplifies the execution of Kusto queries and commands with asynchronous support through embedded .kusto scripts.
- **Paged Query Support**: Efficient handling of large datasets with built-in support for paginated query results through stored query results.

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

_Note: The base types handles the loading of the embedded `.kusto` script file, passing of parameters and deserialization of the output._

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
        [FromHeader(Name = "x-max-item-count")] int? maxItemCount,
        [FromHeader(Name = "x-continuation-token")] string? continuationToken,
        IKustoProcessor processor,
        CancellationToken cancellationToken)
        => await processor.ExecutePagedQuery(
            new CustomersQuery(),
            sessionId,
            maxItemCount ?? 100,
            continuationToken,
            cancellationToken))
    .WithName("GetCustomers")
    .WithOpenApi();
```

The `maxItemCount` specifies how many items to return for each page. Each page is returned with a `continuationToken` that can be specified to fetch the next page.

The optional `sessionId` can be provided to optimize the use of storage on the ADX. If the same `sessionId` is specified for two calls they will share the underlying storage for pagination results.

## Sample

See the [sample api](./sample/Atc.Kusto.Api.Sample/) for an example on how to configure the Atc.Kusto library. The sample api is querying the "ContosoSales" database of the Microsoft ADX sample cluster.

# Requirements

* [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

# How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
