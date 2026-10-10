namespace Atc.Kusto.Factories.Internal;

/// <summary>
/// Creates the Kusto.Data query and admin clients for a connection string.
/// </summary>
/// <remarks>
/// Kept separate from the provider so that client construction can be substituted in tests,
/// for example to assert that concurrent first calls create exactly one client.
/// </remarks>
internal interface IKustoDataClientFactory
{
    /// <summary>
    /// Creates a new query client. The caller owns and disposes it.
    /// </summary>
    /// <param name="connectionString">The connection string, including database and authentication.</param>
    /// <returns>A new <see cref="ICslQueryProvider"/>.</returns>
    ICslQueryProvider CreateQueryClient(
        KustoConnectionStringBuilder connectionString);

    /// <summary>
    /// Creates a new admin client, used for control commands. The caller owns and disposes it.
    /// </summary>
    /// <param name="connectionString">The connection string, including database and authentication.</param>
    /// <returns>A new <see cref="ICslAdminProvider"/>.</returns>
    ICslAdminProvider CreateAdminClient(
        KustoConnectionStringBuilder connectionString);
}