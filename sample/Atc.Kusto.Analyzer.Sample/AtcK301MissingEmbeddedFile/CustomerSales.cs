namespace Atc.Kusto.Analyzer.Sample.AtcK301MissingEmbeddedFile;

public record CustomerSales(
    int CustomerKey,
    string CustomerName,
    decimal SalesAmount,
    decimal TotalCost);