// ReSharper disable CheckNamespace
namespace Atc.Kusto;

public static class ServiceCollectionExtensions
{
    private const int MaxRetryAttempts = 3;

    /// <summary>
    /// Configures the Azure Data Explorer (Kusto) services within the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="hostAddress">The URI of the Azure Data Explorer cluster.</param>
    /// <param name="databaseName">The name of the database within Azure Data Explorer.</param>
    /// <param name="tokenCredential">The token credential used for Azure AD authentication.</param>
    /// <param name="configurationName">
    /// An optional name for the options instance. If provided, the named options will be registered.
    /// </param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection ConfigureAzureDataExplorer(
        this IServiceCollection services,
        Uri hostAddress,
        string databaseName,
        TokenCredential tokenCredential,
        string? configurationName = null)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            services.AddOptions<AtcKustoOptions>().Configure(o =>
            {
                o.HostAddress = hostAddress;
                o.DatabaseName = databaseName;
                o.Credential = tokenCredential;
            });
        }
        else
        {
            services.AddOptions<AtcKustoOptions>(configurationName).Configure(o =>
            {
                o.HostAddress = hostAddress;
                o.DatabaseName = databaseName;
                o.Credential = tokenCredential;
            });
        }

        return services.AddKustoServices();
    }

    /// <summary>
    /// Configures the Azure Data Explorer (Kusto) services within the specified IServiceCollection
    /// using a directly provided AtcKustoOptions instance.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to augment.</param>
    /// <param name="kustoOptions">The pre-configured AtcKustoOptions.</param>
    /// <param name="configurationName">
    /// An optional name for the options instance. If provided, the named options will be registered.
    /// </param>
    /// <returns>The same instance as services.</returns>
    public static IServiceCollection ConfigureAzureDataExplorer(
        this IServiceCollection services,
        AtcKustoOptions kustoOptions,
        string? configurationName = null)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            services.AddOptions<AtcKustoOptions>().Configure(o =>
            {
                o.HostAddress = kustoOptions.HostAddress;
                o.DatabaseName = kustoOptions.DatabaseName;
                o.Credential = kustoOptions.Credential;
            });
        }
        else
        {
            services.AddOptions<AtcKustoOptions>(configurationName).Configure(o =>
            {
                o.HostAddress = kustoOptions.HostAddress;
                o.DatabaseName = kustoOptions.DatabaseName;
                o.Credential = kustoOptions.Credential;
            });
        }

        return services.AddKustoServices();
    }

    /// <summary>
    /// Configures the Azure Data Explorer (Kusto) services within the specified <see cref="IServiceCollection"/>.
    /// using the provided configuration delegate for AtcKustoOptions.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to augment.</param>
    /// <param name="configureOptions">An Action delegate to configure the AtcKustoOptions.</param>
    /// <param name="configurationName">
    /// An optional name for the options instance. If provided, the named options will be registered.
    /// </param>
    /// <returns>The same instance as services.</returns>
    public static IServiceCollection ConfigureAzureDataExplorer(
        this IServiceCollection services,
        Action<AtcKustoOptions> configureOptions,
        string? configurationName = null)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
        {
            services.AddOptions<AtcKustoOptions>().Configure(configureOptions);
        }
        else
        {
            services.AddOptions<AtcKustoOptions>(configurationName).Configure(configureOptions);
        }

        return services.AddKustoServices();
    }

    private static IServiceCollection AddKustoServices(
        this IServiceCollection services)
    {
        services.AddLogging();

        services.AddKeyedSingleton(Constants.ResiliencePipelineKey, ResiliencePipelineImplementationFactory);

        return services
            .AddSingleton<IKustoClientProvider, KustoClientProvider>()
            .AddSingleton<IQueryIdProvider, QueryIdProvider>()
            .AddSingleton<IScriptHandlerFactory, ScriptHandlerFactory>()
            .AddSingleton<IKustoProcessorFactory, KustoProcessorFactory>()
            .AddSingleton(s => s.GetRequiredService<IKustoProcessorFactory>().Create());

        ResiliencePipeline ResiliencePipelineImplementationFactory(
            IServiceProvider serviceProvider,
            object? key)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ResiliencePipeline>>();
            var retryStrategyOptions = new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<KustoServicePartialQueryFailureException>()
                    .Handle<KustoServiceException>()
                    .Handle<Exception>(ex => ex is not OperationCanceledException),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(3),
                OnRetry = args =>
                {
                    var errorMessage = args.Outcome.Exception?.GetLastInnerMessage() ?? "Unknown Exception";

                    logger.LogRetryWarning(
                        errorMessage,
                        args.RetryDelay.TotalSeconds,
                        args.AttemptNumber + 1,
                        MaxRetryAttempts);

                    return ValueTask.CompletedTask;
                },
            };

            return new ResiliencePipelineBuilder()
                .AddRetry(retryStrategyOptions)
                .Build();
        }
    }
}