namespace Atc.Kusto.Api.Sample.Contracts;

public record CustomerSales(
    int CustomerKey,
    string CustomerName,
    float SalesAmount,
    float TotalCost);