// Test: Should report ATCK301 - Missing .kusto file
#pragma warning disable ATCK301

namespace Atc.Kusto.Analyzer.Sample.AtcK301MissingEmbeddedFile;

public record CustomerSalesQuery
    : KustoQuery<CustomerSales>;