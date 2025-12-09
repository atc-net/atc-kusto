// Test: Should report ATCK307 - Result type property 'Email' not projected
#pragma warning disable ATCK307

namespace Atc.Kusto.Analyzer.Sample.AtcK307ResultPropertyNotProjected;

public record CustomersQuery(
    long? CustomerId = null)
    : KustoQuery<Customer>;