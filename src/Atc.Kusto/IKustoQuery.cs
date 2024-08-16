namespace Atc.Kusto;

/// <summary>
/// Defines an interface for Kusto queries that return results of a specified type.
/// Extends the <see cref="IKustoScript"/> interface to include a method for reading query results.
/// </summary>
/// <typeparam name="T">The type of the result returned by the query.</typeparam>
public interface IKustoQuery<out T> : IKustoScript
{
    /// <summary>
    /// Reads and converts the result of a Kusto query from an <see cref="IDataReader"/>
    /// into an instance of type <typeparamref name="T"/>.
    /// Implementations should define how the data is processed and returned.
    /// </summary>
    /// <param name="reader">The <see cref="IDataReader"/> instance used to read the query results.</param>
    /// <returns>An instance of <typeparamref name="T"/> representing the processed query result, or null if no result is available.</returns>
    T? ReadResult(
        IDataReader reader);
}