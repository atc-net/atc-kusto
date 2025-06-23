var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

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

await app.RunAsync();