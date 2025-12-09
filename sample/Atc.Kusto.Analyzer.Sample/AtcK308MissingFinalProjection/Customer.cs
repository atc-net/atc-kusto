namespace Atc.Kusto.Analyzer.Sample.AtcK308MissingFinalProjection;

public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName);