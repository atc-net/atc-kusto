namespace Atc.Kusto.Factories.Internal;

/// <inheritdoc />
internal sealed class KustoDataClientFactory : IKustoDataClientFactory
{
    /// <inheritdoc />
    public ICslQueryProvider CreateQueryClient(
        KustoConnectionStringBuilder connectionString)
        => KustoClientFactory.CreateCslQueryProvider(connectionString);

    /// <inheritdoc />
    public ICslAdminProvider CreateAdminClient(
        KustoConnectionStringBuilder connectionString)
        => KustoClientFactory.CreateCslAdminProvider(connectionString);
}