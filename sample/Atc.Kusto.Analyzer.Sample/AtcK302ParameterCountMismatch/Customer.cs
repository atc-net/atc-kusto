namespace Atc.Kusto.Analyzer.Sample.AtcK302ParameterCountMismatch;

public record Customer(
    long CustomerKey,
    string FirstName,
    string LastName,
    string? CompanyName,
    string CityName,
    string StateProvinceName,
    string RegionCountryName,
    string ContinentName,
    string Gender,
    string MaritalStatus,
    string Education,
    string Occupation);