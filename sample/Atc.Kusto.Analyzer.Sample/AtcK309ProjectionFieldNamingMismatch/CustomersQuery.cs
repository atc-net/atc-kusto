// Test: Should report ATCK309 - Projection field naming mismatch (snake_case vs PascalCase)
#pragma warning disable ATCK309

namespace Atc.Kusto.Analyzer.Sample.AtcK309ProjectionFieldNamingMismatch;

public record CustomersQuery(
    long? CustomerId = null)
    : KustoQuery<Customer>;