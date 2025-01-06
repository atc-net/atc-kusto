var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureAzureDataExplorer(o =>
{
    o.HostAddress = "https://help.kusto.windows.net/";
    o.DatabaseName = "ContosoSales";
    o.Credential = new DefaultAzureCredential();
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

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

app.MapGet(
        "/customers/{customerId}",
        async static (
                long customerId,
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

app.MapGet("/customer-sales", (
            IKustoProcessor processor,
            CancellationToken cancellationToken)
        => processor.ExecuteQuery(
            new CustomerSalesQuery(),
            cancellationToken))
    .WithName("GetCustomerSales")
    .WithOpenApi();

await app.RunAsync();