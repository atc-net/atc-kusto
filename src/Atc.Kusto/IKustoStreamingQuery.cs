namespace Atc.Kusto;

/// <summary>
/// Represents a streaming Kusto query that processes each row from a Kusto data source,
/// mapping the data record to a strongly-typed object of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of each row produced by the query.</typeparam>
public interface IKustoStreamingQuery<out T> : IKustoScript
{
    /// <summary>
    /// Transforms a single result row into an instance of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="record">
    /// The row to be transformed. It is only valid during this call: read the values you need, don't keep the record.
    /// </param>
    /// <returns>
    /// An object of type <typeparamref name="T"/> that represents the row, or <see langword="null"/> to skip the row.
    /// </returns>
    T? MapRecord(IDataRecord record);
}