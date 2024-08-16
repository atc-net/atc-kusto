// ReSharper disable CheckNamespace
namespace Atc.Kusto;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures the Azure Data Explorer (Kusto) services within the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="hostAddress">The URI of the Azure Data Explorer cluster.</param>
    /// <param name="databaseName">The name of the database within Azure Data Explorer.</param>
    /// <param name="tokenCredential">The token credential used for Azure AD authentication.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection ConfigureAzureDataExplorer(
        this IServiceCollection services,
        Uri hostAddress,
        string databaseName,
        TokenCredential tokenCredential)
        => services
            .AddSingleton(_ =>
            {
                var connectionStringBuilder = new KustoConnectionStringBuilder(
                        hostAddress.AbsoluteUri,
                        databaseName)
                    .WithAadAzureTokenCredentialsAuthentication(tokenCredential);

                return KustoClientFactory.CreateCslQueryProvider(connectionStringBuilder);
            })
            .AddSingleton(_ =>
            {
                var connectionStringBuilder = new KustoConnectionStringBuilder(
                        hostAddress.AbsoluteUri,
                        databaseName)
                    .WithAadAzureTokenCredentialsAuthentication(tokenCredential);

                return KustoClientFactory.CreateCslAdminProvider(connectionStringBuilder);
            })
            .AddSingleton<IQueryIdProvider, QueryIdProvider>()
            .AddSingleton<IScriptHandlerFactory, ScriptHandlerFactory>()
            .AddSingleton<IKustoProcessor, KustoProcessor>();
}