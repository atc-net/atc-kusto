namespace Atc.Kusto.Analyzer.Sample.AtcK303ParameterTypeMismatch;

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