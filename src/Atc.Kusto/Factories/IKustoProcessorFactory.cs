namespace Atc.Kusto.Factories;

/// <summary>
/// Factory interface for creating instances of <see cref="IKustoProcessor"/>.
/// </summary>
public interface IKustoProcessorFactory
{
    /// <summary>
    /// Creates an instance of <see cref="IKustoProcessor"/> configured with the specified connection settings.
    /// </summary>
    /// <param name="connectionName">
    /// An optional connection name that identifies the Kusto cluster to be used.
    /// If <see langword="null"/>, the default connection is utilized.
    /// </param>
    /// <param name="databaseName">
    /// An optional database name for the Kusto operations, on the cluster of
    /// <paramref name="connectionName"/>. If <see langword="null"/>, the connection's
    /// <c>AtcKustoOptions.DatabaseName</c> is used; for a connection string without it, the database
    /// in the connection string.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IKustoProcessor"/> that can be used to execute Kusto operations.
    /// </returns>
    IKustoProcessor Create(
        string? connectionName = null,
        string? databaseName = null);
}