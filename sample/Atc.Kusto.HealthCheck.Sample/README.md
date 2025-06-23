# Kusto Health Check

This directory contains a sample application demonstrating how to use the Kusto (Azure Data Explorer) health check functionality in ASP.NET Core applications.

## Overview

The Kusto health check provides a simple way to verify that your Azure Data Explorer cluster is healthy and operational. It uses the `.show diagnostics` command to retrieve cluster health metrics and exposes them through ASP.NET Core's Health Checks API.

## Key Features

- Check cluster health status
- Get detailed health information, including:
  - Whether the cluster is healthy
  - Whether the cluster requires attention
  - Whether the cluster needs to be scaled out
  - Specific diagnostic reasons for health issues

## Usage

### Register the Health Check

In your `Program.cs` or startup class:

```csharp
// Configure Azure Data Explorer
services.ConfigureAzureDataExplorer(
    o =>
    {
        o.HostAddress = new Uri("https://your-adx-cluster-url.kusto.windows.net");
        o.DatabaseName = "YourDatabase";
        o.Credential = new DefaultAzureCredential();
    },
    "DefaultConnection");

// Add health checks
services
    .AddHealthChecks()
    .AddKustoHealthCheck(
        name: "adx", 
        connectionName: "DefaultConnection",
        tags: new[] { "adx", "database" });
```

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
                Data = entry.Value.Data
            })
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options), Encoding.UTF8);
    }
});
```

## Sample Health Check Response

```json
{
  "Status": "Healthy",
  "Duration": "00:00:00.2485141",
  "Checks": [
    {
      "Name": "adx",
      "Status": "Healthy",
      "Duration": "00:00:00.2422320",
      "Description": "Kusto cluster is healthy",
      "Data": {
        "Duration": "00:00:00.2422320",
        "IsAttentionRequired": false,
        "IsScaleOutRequired": false
      }
    }
  ]
}
```

## Status Descriptions

- **Healthy**: The cluster is functioning normally
- **Degraded**: The cluster requires attention but is still operational
- **Unhealthy**: The cluster is not healthy and may not be operational

## Advanced Usage

You can directly use `IKustoHealthCheck` in your own services if you need more control over the health checking process or want to add custom handling based on health check results:

```csharp
public class MyService
{
    private readonly IKustoHealthCheck kustoHealthCheck;

    public MyService(IKustoHealthCheck kustoHealthCheck)
    {
        this.kustoHealthCheck = kustoHealthCheck;
    }

    public async Task DoSomethingWithHealthCheck()
    {
        var healthResult = await kustoHealthCheck.CheckHealthAsync();
        
        if (!healthResult.IsHealthy)
        {
            // Handle unhealthy cluster scenario
            Console.WriteLine($"Cluster unhealthy: {healthResult.NotHealthyReason}");
        }
        else if (healthResult.IsAttentionRequired)
        {
            // Handle cluster that needs attention
            Console.WriteLine($"Cluster needs attention: {healthResult.AttentionRequiredReason}");
        }
        
        // Continue with normal operation
    }
}
```
