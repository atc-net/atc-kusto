namespace Atc.Kusto.Api.Sample.Queries;

public record CustomersStreamingQuery(
    long? CustomerId = null)
    : KustoStreamingQuery<Customer>;