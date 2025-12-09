// Test: Should report ATCK308 - Missing final project statement
#pragma warning disable ATCK308

namespace Atc.Kusto.Analyzer.Sample.AtcK308MissingFinalProjection;

public record CustomersQuery(
    long? CustomerId = null)
    : KustoQuery<Customer>;