// Test: Should report ATCK302 - Parameter count mismatch
#pragma warning disable ATCK302

namespace Atc.Kusto.Analyzer.Sample.AtcK302ParameterCountMismatch;

public record CustomersQuery(
    long? CustomerId = null)
    : KustoQuery<Customer>;