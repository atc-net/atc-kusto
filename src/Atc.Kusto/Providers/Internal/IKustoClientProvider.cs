namespace Atc.Kusto.Providers.Internal;

/// <summary>
/// Provides functionality to retrieve Kusto clients for administrative operations and query executions.
/// This interface defines methods to obtain an instance of <see cref="ICslAdminProvider"/> and <see cref="ICslQueryProvider"/>
/// based on optional connection name and database name parameters.
/// </summary>
public interface IKustoClientProvider
{
    /// <summary>
    /// Retrieves an instance of <see cref="ICslAdminProvider"/> for executing administrative operations against Kusto.
    /// </summary>
    /// <param name="connectionName">
    /// The optional connection name identifying the Kusto connection to be used. If null, a default connection is assumed.
    /// </param>
    /// <param name="databaseName">
    /// The optional database name to be used. If null, the default database for the connection is used.
    /// </param>
    /// <returns>
    /// An instance of <see cref="ICslAdminProvider"/> configured for the specified connection and database.
    /// </returns>
    ICslAdminProvider GetAdminClient(
        string? connectionName = null,
        string? databaseName = null);

    /// <summary>
    /// Retrieves an instance of <see cref="ICslQueryProvider"/> for executing queries against Kusto.
    /// </summary>
    /// <param name="connectionName">
    /// The optional connection name identifying the Kusto connection to be used. If null, a default connection is assumed.
    /// </param>
    /// <param name="databaseName">
    /// The optional database name to be used. If null, the default database for the connection is used.
    /// </param>
    /// <returns>
    /// An instance of <see cref="ICslQueryProvider"/> configured for the specified connection and database.
    /// </returns>
    ICslQueryProvider GetQueryClient(
        string? connectionName = null,
        string? databaseName = null);
}