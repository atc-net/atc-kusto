namespace Atc.Kusto.Sample.Queries;

/// <summary>
/// Maps each row itself, to "FirstName LastName", instead of to a <see cref="Customer"/>.
/// </summary>
public record CustomerNamesStreamingQuery
    : KustoStreamingQuery<string>
{
    public override string? MapRecord(
        IDataRecord record,
        JsonSerializerOptions serializerOptions)
        => $"{record.GetString(record.GetOrdinal("FirstName"))} {record.GetString(record.GetOrdinal("LastName"))}";
}