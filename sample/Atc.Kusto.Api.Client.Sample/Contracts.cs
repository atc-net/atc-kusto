namespace Atc.Kusto.Api.Client.Sample;

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

public record NycTaxiTrip(
    DateTime DropoffDatetime,
    double DropoffLatitude,
    double DropoffLongitude,
    double FareAmount,
    double MtaTax,
    int PassengerCount,
    string PaymentType,
    DateTime PickupDatetime,
    double PickupLatitude,
    double PickupLongitude,
    string RateCode,
    string StoreAndFwdFlag,
    double Surcharge,
    double TipAmount,
    double TollsAmount,
    double TotalAmount,
    double TripDistance,
    string VendorId);

public class KustoDataSetHeader
{
    public string Version { get; set; } = string.Empty;

    public bool IsProgressive { get; set; }
}

public class KustoTableSchema
{
    public string TableName { get; set; } = string.Empty;

    public List<KustoColumn> Columns { get; set; } = new();
}

public class KustoColumn
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
}

public class KustoResultCompletion
{
    public bool HasErrors { get; set; }

    public string? ErrorMessage { get; set; }

    public List<KustoTableCompletionInfo> TableCompletions { get; set; } = new();
}

public class KustoTableCompletionInfo
{
    public int TableId { get; set; }

    public int RowCount { get; set; }

    public string? Exception { get; set; }
}

public class StreamingQueryResult<T>
{
    public KustoDataSetHeader? Header { get; set; }

    public List<KustoTableSchema> TableSchemas { get; set; } = new();

    public KustoResultCompletion? Completion { get; set; }

    public List<T> Rows { get; set; } = new();
}