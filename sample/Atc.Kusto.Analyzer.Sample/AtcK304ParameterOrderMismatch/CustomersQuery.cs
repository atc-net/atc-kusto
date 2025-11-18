// Test: Should report ATCK304 - Parameter order mismatch (Testing/CustomerId vs customerId/testing)
#pragma warning disable ATCK304

namespace Atc.Kusto.Analyzer.Sample.AtcK304ParameterOrderMismatch;

public record CustomersQuery(
    string? Testing = null,
    long? CustomerId = null)
    : KustoQuery<Customer>;