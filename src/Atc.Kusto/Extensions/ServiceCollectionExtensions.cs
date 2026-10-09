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
        ArgumentNullException.ThrowIfNull(kustoOptions);

        if (string.IsNullOrWhiteSpace(configurationName))
        {
            services.AddOptions<AtcKustoOptions>().Configure(o => CopyOptions(kustoOptions, o));
        }
        else
        {
            services.AddOptions<AtcKustoOptions>(configurationName).Configure(o => CopyOptions(kustoOptions, o));
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

    /// <summary>
    /// Copies every setting from a caller-supplied options instance onto the registered one.
    /// </summary>
    /// <remarks>
    /// Copy every property here: anything missed is silently dropped for callers using the
    /// <see cref="ConfigureAzureDataExplorer(IServiceCollection, AtcKustoOptions, string?)"/> overload.
    /// </remarks>
    private static void CopyOptions(
        AtcKustoOptions source,
        AtcKustoOptions target)
    {
        target.HostAddress = source.HostAddress;
        target.DatabaseName = source.DatabaseName;
        target.Credential = source.Credential;
        target.ConnectionString = source.ConnectionString;
        target.DefaultIngestionMode = source.DefaultIngestionMode;

        foreach (var container in source.IngestUploadContainers)
        {
            target.IngestUploadContainers.Add(container);
        }
    }

    /// <summary>
    /// Registers the shared Kusto services.
    /// </summary>
    /// <remarks>
    /// Runs on every <c>ConfigureAzureDataExplorer</c> call (once per named connection), so every
    /// registration uses <c>TryAdd</c>: the first call registers, later calls are no-ops, and services
    /// a consumer registered beforehand are respected.
    /// </remarks>
    private static IServiceCollection AddKustoServices(
        this IServiceCollection services)
    {
        services.AddLogging();

        services.TryAddKeyedSingleton(Constants.ResiliencePipelineKey, ResiliencePipelineImplementationFactory);

        services.TryAddSingleton<KustoClientProvider>();
        services.TryAddSingleton<IKustoClientProvider>(s => s.GetRequiredService<KustoClientProvider>());
        services.TryAddSingleton<IKustoIngestClientProvider>(s => s.GetRequiredService<KustoClientProvider>());

        services.TryAddSingleton<IQueryIdProvider, QueryIdProvider>();
        services.TryAddSingleton<IScriptHandlerFactory, ScriptHandlerFactory>();
        services.TryAddSingleton<IKustoProcessorFactory, KustoProcessorFactory>();
        services.TryAddSingleton(s => s.GetRequiredService<IKustoProcessorFactory>().Create());

        // Ingestion has no resilience pipeline on purpose: the Ingest V2 SDK retries transient
        // failures itself, and retrying a write on top of that risks duplicate ingestion.
        services.TryAddSingleton<IKustoIngestor, KustoIngestor>();

        return services;

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
                    .Handle<Exception>(ex => !CancellationExceptionUtilities.IsCancellationException(ex)),
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