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
    {
        services
            .AddOptions<AtcKustoOptions>()
            .Configure(o =>
            {
                o.HostAddress = hostAddress;
                o.DatabaseName = databaseName;
                o.Credential = tokenCredential;
            });

        return services.AddKustoServices();
    }

    /// <summary>
    /// Configures the Azure Data Explorer (Kusto) services within the specified IServiceCollection
    /// using a directly provided AtcKustoOptions instance.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to augment.</param>
    /// <param name="kustoOptions">The pre-configured AtcKustoOptions.</param>
    /// <returns>The same instance as services.</returns>
    public static IServiceCollection ConfigureAzureDataExplorer(
        this IServiceCollection services,
        AtcKustoOptions kustoOptions)
    {
        services
            .AddOptions<AtcKustoOptions>()
            .Configure(o =>
            {
                o.HostAddress = kustoOptions.HostAddress;
                o.DatabaseName = kustoOptions.DatabaseName;
                o.Credential = kustoOptions.Credential;
            });

        return services.AddKustoServices();
    }

    /// <summary>
    /// Configures the Azure Data Explorer (Kusto) services within the specified <see cref="IServiceCollection"/>.
    /// using the provided configuration delegate for AtcKustoOptions.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to augment.</param>
    /// <param name="configureOptions">An Action delegate to configure the AtcKustoOptions.</param>
    /// <returns>The same instance as services.</returns>
    public static IServiceCollection ConfigureAzureDataExplorer(
        this IServiceCollection services,
        Action<AtcKustoOptions> configureOptions)
    {
        services
            .AddOptions<AtcKustoOptions>()
            .Configure(configureOptions);

        return services.AddKustoServices();
    }

    private static IServiceCollection AddKustoServices(
        this IServiceCollection services)
        => services
            .AddSingleton<IKustoClientProvider, KustoClientProvider>()
            .AddSingleton<IQueryIdProvider, QueryIdProvider>()
            .AddSingleton<IScriptHandlerFactory, ScriptHandlerFactory>()
            .AddSingleton<IKustoProcessor, KustoProcessor>();
}