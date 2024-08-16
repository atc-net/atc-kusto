namespace Atc.Kusto;

/// <summary>
/// Represents an abstract base class for executing Kusto queries that return results of a specified type.
/// Inherits from <see cref="KustoScript"/> and implements <see cref="IKustoQuery{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the result returned by the query.</typeparam>
public abstract record KustoQuery<T> : KustoScript, IKustoQuery<T>
{
    /// <inheritdoc />>
    public abstract T? ReadResult(
        IDataReader reader);
}