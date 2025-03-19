namespace Atc.Kusto;
/// <summary>
/// Abstract base class for streaming queries.
/// Inherit from this class when each record should map to a single T.
/// </summary>
/// <typeparam name="T">The type of each row in the result.</typeparam>
public abstract record KustoStreamingQuery<T> : KustoScript, IKustoStreamingQuery<T>
{
    /// <summary>
    /// Maps the current data row to an object of type <typeparamref name="T"/>.
    /// By default, this uses the JSON-based conversion.
    /// Override this method if you need custom mapping.
    /// </summary>
    /// <param name="row">The data row.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public virtual T? MapDataRow(DataRow row)
        => row.MapDataRow<T>();
}