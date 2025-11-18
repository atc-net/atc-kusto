// Test: Should report ATCK303 - Parameter type mismatch (long vs string)
#pragma warning disable ATCK303

namespace Atc.Kusto.Analyzer.Sample.AtcK303ParameterTypeMismatch;

public record CustomersQuery(
    long? CustomerId = null)
    : KustoQuery<Customer>;