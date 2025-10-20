namespace Atc.Kusto.Sample.Queries;

public record CustomerSalesStreamingQuery(
    long? CustomerKey = null)
    : KustoStreamingQuery<CustomerSales>;