namespace Atc.Kusto.Sample.Queries;

public record CustomersSplitByGenderQuery
    : KustoScript, IKustoQuery<CustomersByGender>
{
    public CustomersByGender ReadResult(
        IDataReader reader,
        JsonSerializerOptions serializerOptions)
        => new(
            reader.ReadObjects<Customer>(serializerOptions),
            reader.ReadObjectsFromNextResult<Customer>(serializerOptions),
            reader.ReadObjectsFromNextResult<CustomerGenderCount>(serializerOptions));
}