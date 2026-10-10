namespace Atc.Kusto;
/// <summary>
/// Abstract base class for streaming queries.
/// Inherit from this class when each record should map to a single T.
/// </summary>
/// <typeparam name="T">The type of each row in the result.</typeparam>
public abstract record KustoStreamingQuery<T> : KustoScript, IKustoStreamingQuery<T>
{
    /// <summary>
    /// Maps the current result row to an object of type <typeparamref name="T"/>.
    /// By default, this uses the same JSON-based conversion as <see cref="KustoQuery{T}"/>
    /// (<see cref="DataReaderExtensions.ReadObject{T}"/>).
    /// Override this method if you need custom mapping.
    /// </summary>
    /// <param name="record">
    /// The row. It is only valid during this call: read the values you need, don't keep the record.
    /// </param>
    /// <returns>An instance of <typeparamref name="T"/>, or <see langword="null"/> to skip the row.</returns>
    public virtual T? MapRecord(IDataRecord record)
        => record.ReadObject<T>();
}