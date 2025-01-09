namespace Atc.Kusto.Sample.Queries;

public record CustomerByIdQuery(long CustomerId)
    : KustoQuery<Customer>;