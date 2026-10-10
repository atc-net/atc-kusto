namespace Atc.Kusto.Tests.Providers.Internal;

public sealed class KustoClientProviderClientCacheTests
{
    private static readonly AtcKustoOptions Options = new()
    {
        HostAddress = new Uri("https://example.kusto.windows.net"),
        DatabaseName = "Db",
    };

    [Theory]
    [InlineAutoNSubstituteData(false)]
    [InlineAutoNSubstituteData(true)]
    internal void Concurrent_First_Calls_Create_One_Client(
        bool admin,
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory ingestClientFactory,
        IKustoDataClientFactory dataClientFactory)
    {
        // Arrange
        monitor.Get(null).Returns(Options);

        var creations = 0;
        using var neverSet = new ManualResetEventSlim();
        ReturnNewClients(dataClientFactory, () =>
        {
            Interlocked.Increment(ref creations);

            // Widen the race window so an unsynchronised cache would create more than one client.
            neverSet.Wait(TimeSpan.FromMilliseconds(50));
        });

        using var sut = new KustoClientProvider(monitor, ingestClientFactory, dataClientFactory);

        var clients = new IDisposable[8];
        using var start = new ManualResetEventSlim();
        var threads = Enumerable
            .Range(0, clients.Length)
            .Select(i => new Thread(() =>
            {
                start.Wait();
                clients[i] = GetClient(sut, admin);
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

    [Theory]
    [InlineAutoNSubstituteData(false)]
    [InlineAutoNSubstituteData(true)]
    internal void A_Failed_Creation_Is_Not_Cached(
        bool admin,
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory ingestClientFactory,
        IKustoDataClientFactory dataClientFactory)
    {
        // Arrange - the first call finds no configuration, the second a corrected one
        monitor.Get(null).Returns(new AtcKustoOptions(), Options);
        ReturnNewClients(dataClientFactory);

        using var sut = new KustoClientProvider(monitor, ingestClientFactory, dataClientFactory);

        var failingCall = () => GetClient(sut, admin);
        failingCall.Should().Throw<InvalidOperationException>();

        // Act
        var client = GetClient(sut, admin);

        // Assert
        client.Should().NotBeNull();
        GetClient(sut, admin).Should().BeSameAs(client);
    }

    [Theory, AutoNSubstituteData]
    internal void Each_Database_Gets_Its_Own_Client(
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory ingestClientFactory,
        IKustoDataClientFactory dataClientFactory)
    {
        // Arrange
        monitor.Get(null).Returns(Options);
        ReturnNewClients(dataClientFactory);

        using var sut = new KustoClientProvider(monitor, ingestClientFactory, dataClientFactory);

        // Act
        var defaultDatabase = sut.GetQueryClient();
        var otherDatabase = sut.GetQueryClient(databaseName: "Other");

        // Assert
        otherDatabase.Should().NotBeSameAs(defaultDatabase);
        sut.GetQueryClient(databaseName: "Other").Should().BeSameAs(otherDatabase);
    }

    [Theory, AutoNSubstituteData]
    internal void Dispose_Disposes_Each_Created_Client_Once(
        IOptionsMonitor<AtcKustoOptions> monitor,
        IKustoIngestClientFactory ingestClientFactory,
        IKustoDataClientFactory dataClientFactory)
    {
        // Arrange
        monitor.Get(null).Returns(Options);
        ReturnNewClients(dataClientFactory);

        var sut = new KustoClientProvider(monitor, ingestClientFactory, dataClientFactory);
        var queryClient = sut.GetQueryClient();
        var adminClient = sut.GetAdminClient();

        // Act
        sut.Dispose();
        sut.Dispose();

        // Assert
        queryClient.Received(1).Dispose();
        adminClient.Received(1).Dispose();
    }

    private static IDisposable GetClient(
        KustoClientProvider sut,
        bool admin)
        => admin
            ? sut.GetAdminClient()
            : sut.GetQueryClient();

    private static void ReturnNewClients(
        IKustoDataClientFactory dataClientFactory,
        Action? onCreate = null)
    {
        dataClientFactory
            .CreateQueryClient(Arg.Any<KustoConnectionStringBuilder>())
            .Returns(_ =>
            {
                onCreate?.Invoke();
                return Substitute.For<ICslQueryProvider>();
            });

        dataClientFactory
            .CreateAdminClient(Arg.Any<KustoConnectionStringBuilder>())
            .Returns(_ =>
            {
                onCreate?.Invoke();
                return Substitute.For<ICslAdminProvider>();
            });
    }
}