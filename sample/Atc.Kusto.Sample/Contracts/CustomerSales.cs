namespace Atc.Kusto.Sample.Contracts;

public record CustomerSales(
    int CustomerKey,
    string CustomerName,
    decimal SalesAmount,
    decimal TotalCost);