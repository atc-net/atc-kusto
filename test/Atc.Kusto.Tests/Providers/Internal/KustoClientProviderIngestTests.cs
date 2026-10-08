namespace Atc.Kusto.Tests.Providers.Internal;

public sealed class KustoClientProviderIngestTests
{
    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetIngestClient_Returns_Client_When_HostAddress_And_Credential_Are_Present(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var client = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.ManagedStreaming);

        // Assert
        Assert.NotNull(client);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetIngestClient_Throws_When_Credential_Is_Missing(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var act = () => ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.Queued);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HostAddress and Credential*");
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetIngestClient_Throws_When_HostAddress_Is_Missing(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        monitor.Get(null).Returns(options);

        // Act
        var act = () => ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.ManagedStreaming);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HostAddress and Credential*");
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetIngestClient_Caches_Per_Connection_And_Mode(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var first = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.ManagedStreaming);
        var second = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.ManagedStreaming);

        // Assert
        Assert.Same(first, second);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetIngestClient_Returns_Distinct_Clients_Per_Mode(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var managed = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.ManagedStreaming);
        var queued = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.Queued);

        // Assert
        Assert.NotSame(managed, queued);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetIngestClient_Ignores_Database_So_Cache_Is_Not_Duplicated(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var first = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.Streaming);
        options.DatabaseName = "ADifferentDatabase";
        var second = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.Streaming);

        // Assert
        Assert.Same(first, second);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetIngestClient_Returns_Distinct_Clients_Per_Connection(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);
        monitor.Get(connectionName).Returns(options);

        // Act
        var defaultClient = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.ManagedStreaming);
        var namedClient = ((IKustoIngestClientProvider)sut).GetIngestClient(IngestionMode.ManagedStreaming, connectionName);

        // Assert
        Assert.NotSame(defaultClient, namedClient);
    }
}