// Test: Should report ATCK306 - Projection field 'UnknownField' not found in result type
#pragma warning disable ATCK306

namespace Atc.Kusto.Analyzer.Sample.AtcK306ProjectionFieldNotFound;

public record CustomersQuery(
    long? CustomerId = null)
    : KustoQuery<Customer>;