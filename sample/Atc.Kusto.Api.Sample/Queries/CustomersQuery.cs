namespace Atc.Kusto.Api.Sample.Queries;

public record CustomersQuery(
    long? CustomerId = null)
    : KustoQuery<Customer>;