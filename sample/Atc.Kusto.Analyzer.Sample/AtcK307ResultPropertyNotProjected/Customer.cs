namespace Atc.Kusto.Analyzer.Sample.AtcK307ResultPropertyNotProjected;

public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName,
    string Email);