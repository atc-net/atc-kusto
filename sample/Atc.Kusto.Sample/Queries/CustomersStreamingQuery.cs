namespace Atc.Kusto.Sample.Queries;

public record CustomersStreamingQuery(
    long? CustomerId = null)
    : KustoStreamingQuery<Customer>;