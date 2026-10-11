# Kusto Health Check

This sample shows the Kusto (Azure Data Explorer) cluster health check in an ASP.NET Core application. It checks the `ContosoSales` database on the Microsoft ADX sample cluster (`https://help.kusto.windows.net`).

## Overview

The health check runs `.show diagnostics` on the cluster and reports the result through ASP.NET Core's Health Checks API:

- whether the cluster is healthy, and why not;
- whether the cluster requires attention, and why;
- whether scaling out the cluster is recommended.

## Usage

### Register the Health Check

In `Program.cs`:

```csharp
// Configure Azure Data Explorer
builder.Services.ConfigureAzureDataExplorer(
    o =>
    {
        o.HostAddress = new Uri("https://help.kusto.windows.net/");
        o.DatabaseName = "ContosoSales";
        o.Credential = new DefaultAzureCredential();
    },
    "ContosoSales");

// Add health checks
builder.Services
    .AddHealthChecks()
    .AddKustoClusterDiagnosticsHealthCheck(
        name: "adx-contososales",
        connectionName: "ContosoSales",
        tags: ["adx", "database"]);
```

`AddKustoClusterDiagnosticsHealthCheck` also takes `databaseName`, `failureStatus` and `timeout`. See the [Health checks](../../README.md#health-checks) section of the main README.

### Add Health Check Endpoint

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration,
                Description = entry.Value.Description,
                Data = entry.Value.Data,
            }),
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options), Encoding.UTF8);
    },
});
```

## Sample Health Check Response

`GET /health`:

```json
{
  "Status": "Healthy",
  "Duration": "00:00:00.2485141",
  "Checks": [
    {
      "Name": "adx-contososales",
      "Status": "Healthy",
      "Duration": "00:00:00.2422320",
      "Description": "Kusto cluster is healthy",
      "Data": {
        "duration": "00:00:00.2422320",
        "isAttentionRequired": false,
        "isScaleOutRequired": false
      }
    }
  ]
}
```

The outer property names come from the anonymous type in the response writer. The `Data` keys come from the health check, which writes them in camelCase. `Data` also contains `notHealthyReason` and `attentionRequiredReason` when there is a reason.

## Status Descriptions

- **Healthy**: The cluster is functioning normally (also when scaling out is recommended).
- **Degraded**: The cluster requires attention but is still operational.
- **Unhealthy** (or the registration's `failureStatus`): The cluster is not healthy, or the check failed, for example because the cluster can't be reached.

## Advanced Usage

`ConfigureAzureDataExplorer(...)` also registers `IKustoClusterDiagnosticsHealthCheck`, so you can check the cluster from your own services:

```csharp
public sealed class ClusterStatusService(
    IKustoClusterDiagnosticsHealthCheck healthCheck)
{
    public async Task LogClusterStatusAsync(
        CancellationToken cancellationToken)
    {
        var result = await healthCheck.CheckHealthAsync(
            connectionName: "ContosoSales",
            cancellationToken: cancellationToken);

        if (!result.IsHealthy)
        {
            Console.WriteLine($"Cluster unhealthy: {result.NotHealthyReason}");
        }
        else if (result.IsAttentionRequired)
        {
            Console.WriteLine($"Cluster needs attention: {result.AttentionRequiredReason}");
        }
    }
}
```

`CheckHealthAsync` doesn't throw when the cluster is unhealthy or can't be reached: it returns a result with `IsHealthy = false` and the reason. It only throws `OperationCanceledException` when your token is cancelled.
