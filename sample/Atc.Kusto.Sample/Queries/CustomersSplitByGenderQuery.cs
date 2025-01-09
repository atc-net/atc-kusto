namespace Atc.Kusto.Sample.Queries;

public record CustomersSplitByGenderQuery
    : KustoScript, IKustoQuery<CustomersByGender>
{
    public CustomersByGender ReadResult(IDataReader reader)
        => new(
            reader.ReadObjects<Customer>(),
            reader.ReadObjectsFromNextResult<Customer>(),
            reader.ReadObjectsFromNextResult<CustomerGenderCount>());
}