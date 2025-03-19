namespace Atc.Kusto;

/// <summary>
/// Represents a streaming Kusto query that processes each row from a Kusto data source,
/// mapping the data record to a strongly-typed object of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of each row produced by the query.</typeparam>
public interface IKustoStreamingQuery<out T> : IKustoScript
{
    /// <summary>
    /// Transforms a single data row into an instance of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="row">The data row to be transformed.</param>
    /// <returns>An object of type <typeparamref name="T"/> that represents the mapped data record.</returns>
    T? MapDataRow(DataRow row);
}