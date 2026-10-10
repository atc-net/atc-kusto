namespace Atc.Kusto.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    private static readonly Uri HostAddress = new("https://example.kusto.windows.net");

    [Theory, AutoNSubstituteData]
    public void IKustoIngestor_Is_Resolvable_Without_A_Separate_Opt_In(
        Azure.Core.TokenCredential credential)
    {
        // Arrange
        var services = new ServiceCollection();
        services.ConfigureAzureDataExplorer(HostAddress, "Db", credential);

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IKustoIngestor>().Should().NotBeNull();
    }

    [Theory, AutoNSubstituteData]
    public void Query_And_Ingest_Provider_Interfaces_Share_One_Instance(
        Azure.Core.TokenCredential credential)
    {
        // Arrange
        var services = new ServiceCollection();
        services.ConfigureAzureDataExplorer(HostAddress, "Db", credential);

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        var queryProvider = provider.GetRequiredService<IKustoClientProvider>();
        var ingestProvider = provider.GetRequiredService<IKustoIngestClientProvider>();
        queryProvider.Should().BeSameAs(ingestProvider);
    }

    [Theory, AutoNSubstituteData]
    public void Configuring_Several_Connections_Registers_Shared_Services_Once(
        Azure.Core.TokenCredential credential)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.ConfigureAzureDataExplorer(HostAddress, "Db", credential);
        services.ConfigureAzureDataExplorer(HostAddress, "Sales", credential, "Sales");
        services.ConfigureAzureDataExplorer(o => o.ConnectionString = "Data Source=https://other.kusto.windows.net", "Other");

        // Assert
        services.Count(d => d.ServiceType == typeof(KustoClientProvider)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(IKustoClientProvider)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(IKustoIngestClientProvider)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(IKustoIngestor)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(IKustoProcessor)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(ResiliencePipeline)).Should().Be(1);
    }

    [Theory, AutoNSubstituteData]
    public void A_Service_Registered_By_The_Consumer_Beforehand_Is_Kept(
        IKustoIngestor customIngestor,
        Azure.Core.TokenCredential credential)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(customIngestor);

        // Act
        services.ConfigureAzureDataExplorer(HostAddress, "Db", credential);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IKustoIngestor>().Should().BeSameAs(customIngestor);
    }

    [Theory]
    [InlineAutoNSubstituteData(null)]
    [InlineAutoNSubstituteData("Sales")]
    public void Options_Instance_Overload_Copies_Every_Setting(
        string? configurationName,
        Azure.Core.TokenCredential credential)
    {
        // Arrange
        var container = new Uri("https://account.blob.core.windows.net/ingest");
        var options = new AtcKustoOptions
        {
            HostAddress = HostAddress,
            DatabaseName = "Db",
            Credential = credential,
            ConnectionString = "Data Source=https://example.kusto.windows.net",
            DefaultIngestionMode = IngestionMode.Queued,
        };

        options.IngestUploadContainers.Add(container);

        var services = new ServiceCollection();
        services.ConfigureAzureDataExplorer(options, configurationName);

        // Act
        using var provider = services.BuildServiceProvider();
        var registered = provider
            .GetRequiredService<IOptionsMonitor<AtcKustoOptions>>()
            .Get(configurationName ?? Microsoft.Extensions.Options.Options.DefaultName);

        // Assert
        registered.HostAddress.Should().Be(HostAddress);
        registered.DatabaseName.Should().Be("Db");
        registered.Credential.Should().BeSameAs(credential);
        registered.ConnectionString.Should().Be("Data Source=https://example.kusto.windows.net");
        registered.DefaultIngestionMode.Should().Be(IngestionMode.Queued);
        registered.IngestUploadContainers.Should().ContainSingle().Which.Should().Be(container);
    }
}