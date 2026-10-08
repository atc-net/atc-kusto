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

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetIngestClient_Creates_One_Client_When_Called_Concurrently(
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory factory,
        AtcKustoOptions options)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        var creations = 0;
        using var neverSet = new ManualResetEventSlim();
        factory
            .Create(
                clusterUri: null,
                credential: null,
                uploadContainers: null,
                mode: default)
            .ReturnsForAnyArgs(_ =>
            {
                Interlocked.Increment(ref creations);

                // Widen the race window so an unsynchronised cache would create more than one client.
                neverSet.Wait(TimeSpan.FromMilliseconds(50));
                return Substitute.For<IKustoIngestClient>();
            });

        using var sut = new KustoClientProvider(monitor, factory);
        var provider = (IKustoIngestClientProvider)sut;

        var clients = new IKustoIngestClient[8];
        using var start = new ManualResetEventSlim();
        var threads = Enumerable
            .Range(0, clients.Length)
            .Select(i => new Thread(() =>
            {
                start.Wait();
                clients[i] = provider.GetIngestClient(IngestionMode.Queued);
            }))
            .ToList();

        // Act
        threads.ForEach(t => t.Start());
        start.Set();
        threads.ForEach(t => t.Join());

        // Assert
        creations.Should().Be(1);
        clients.Should().AllSatisfy(c => c.Should().BeSameAs(clients[0]));
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetIngestClient_Does_Not_Cache_A_Failed_Creation(
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory factory,
        AtcKustoOptions options,
        Azure.Core.TokenCredential credential)
    {
        // Arrange
        monitor.Get(null).Returns(options);
        using var sut = new KustoClientProvider(monitor, factory);
        var provider = (IKustoIngestClientProvider)sut;

        var failingCall = () => provider.GetIngestClient(IngestionMode.Streaming);
        failingCall.Should().Throw<InvalidOperationException>();

        options.Credential = credential;

        // Act
        var client = provider.GetIngestClient(IngestionMode.Streaming);

        // Assert
        client.Should().NotBeNull();
        factory
            .ReceivedWithAnyArgs(1)
            .Create(
                clusterUri: null,
                credential: null,
                uploadContainers: null,
                mode: default);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void Dispose_Disposes_Created_Ingest_Clients(
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory factory,
        AtcKustoOptions options)
    {
        // Arrange
        monitor.Get(null).Returns(options);
        factory
            .Create(
                clusterUri: null,
                credential: null,
                uploadContainers: null,
                mode: default)
            .ReturnsForAnyArgs(_ => Substitute.For<IKustoIngestClient>());

        var sut = new KustoClientProvider(monitor, factory);
        var provider = (IKustoIngestClientProvider)sut;
        var streaming = provider.GetIngestClient(IngestionMode.Streaming);
        var queued = provider.GetIngestClient(IngestionMode.Queued);

        // Act
        sut.Dispose();

        // Assert
        streaming.Received(1).Dispose();
        queued.Received(1).Dispose();
    }
}