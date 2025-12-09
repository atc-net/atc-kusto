namespace Atc.Kusto.Analyzer.Sample.AtcK309ProjectionFieldNamingMismatch;

public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName);