// Test: Should report ATCK305 - Empty .kusto file
#pragma warning disable ATCK305

namespace Atc.Kusto.Analyzer.Sample.AtcK305EmptyKustoFile;

public record CustomersQuery(
    long? CustomerId = null)
    : KustoQuery<Customer>;