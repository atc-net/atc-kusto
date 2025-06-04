namespace Atc.Kusto.Tests.Providers.Internal;

[SuppressMessage("SonarAnalyzer.CSharp", "S4144", Justification = "OK.")]
public sealed class KustoClientProviderTests
{
    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetQueryClient_Returns_Client_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetQueryClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetQueryClient_Returns_Client_With_Connection_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(connectionName).Returns(options);

        // Act
        var client = sut.GetQueryClient(connectionName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetQueryClient_Returns_Client_With_Database_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string databaseName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetQueryClient(databaseName: databaseName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(databaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetQueryClient_Returns_Client_With_Connection_And_Database_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        string databaseName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(connectionName).Returns(options);

        // Act
        var client = sut.GetQueryClient(connectionName, databaseName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(databaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetQueryClient_Returns_Client_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetQueryClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetQueryClient_Returns_Client_With_Connection_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(connectionName).Returns(options);

        // Act
        var client = sut.GetQueryClient(connectionName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetQueryClient_Returns_Client_With_Database_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string databaseName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetQueryClient(databaseName: databaseName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(databaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetQueryClient_Returns_Client_With_Connection_And_Database_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        string databaseName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(connectionName).Returns(options);

        // Act
        var client = sut.GetQueryClient(connectionName, databaseName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(databaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetQueryClient_Returns_Client_With_ConnectionString_And_Database_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        options.ConnectionString = $"https://{Guid.NewGuid():N}.kusto.windows.net";
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetQueryClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetQueryClient_Returns_Client_With_ConnectionString_And_Database_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        options.ConnectionString = $"https://{Guid.NewGuid():N}.kusto.windows.net";
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetQueryClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetQueryClient_Returns_Client_With_ConnectionString_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        options.ConnectionString = $"https://{Guid.NewGuid():N}.kusto.windows.net";
        options.DatabaseName = null;
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetQueryClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal("NetDefaultDB", client.DefaultDatabaseName); // Apparently the library still sets a default database name
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetQueryClient_Returns_Client_With_ConnectionString_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        options.ConnectionString = $"https://{Guid.NewGuid():N}.kusto.windows.net";
        options.DatabaseName = null;
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetQueryClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal("NetDefaultDB", client.DefaultDatabaseName); // Apparently the library still sets a default database name
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetAdminClient_Returns_Client_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        var client = sut.GetAdminClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetAdminClient_Returns_Client_With_Connection_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(connectionName).Returns(options);

        // Act
        var client = sut.GetAdminClient(connectionName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetAdminClient_Returns_Client_With_Database_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string databaseName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetAdminClient(databaseName: databaseName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(databaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetAdminClient_Returns_Client_With_Connection_And_Database_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        string databaseName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(connectionName).Returns(options);

        // Act
        var client = sut.GetAdminClient(connectionName, databaseName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(databaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetAdminClient_Returns_Client_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetAdminClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetAdminClient_Returns_Client_With_Connection_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(connectionName).Returns(options);

        // Act
        var client = sut.GetAdminClient(connectionName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetAdminClient_Returns_Client_With_Database_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string databaseName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetAdminClient(databaseName: databaseName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(databaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetAdminClient_Returns_Client_With_Connection_And_Database_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        string connectionName,
        string databaseName,
        KustoClientProvider sut)
    {
        // Arrange
        monitor.Get(connectionName).Returns(options);

        // Act
        var client = sut.GetAdminClient(connectionName, databaseName);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(databaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetAdminClient_Returns_Client_With_ConnectionString_And_Database_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        options.ConnectionString = $"https://{Guid.NewGuid():N}.kusto.windows.net";
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetAdminClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetAdminClient_Returns_Client_With_ConnectionString_And_Database_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        options.ConnectionString = $"https://{Guid.NewGuid():N}.kusto.windows.net";
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetAdminClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(options.DatabaseName, client.DefaultDatabaseName);
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: true)]
    internal void GetAdminClient_Returns_Client_With_ConnectionString_With_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        options.ConnectionString = $"https://{Guid.NewGuid():N}.kusto.windows.net";
        options.DatabaseName = null;
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetAdminClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal("NetDefaultDB", client.DefaultDatabaseName); // Apparently the library still sets a default database name
    }

    [Theory, AutoNSubstituteDataWithAtcKustoOptions(withCredential: false)]
    internal void GetAdminClient_Returns_Client_With_ConnectionString_Without_Credential(
        [Frozen] IOptionsMonitor<AtcKustoOptions> monitor,
        AtcKustoOptions options,
        KustoClientProvider sut)
    {
        // Arrange
        options.HostAddress = null;
        options.ConnectionString = $"https://{Guid.NewGuid():N}.kusto.windows.net";
        options.DatabaseName = null;
        monitor.Get(null).Returns(options);

        // Act
        var client = sut.GetAdminClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal("NetDefaultDB", client.DefaultDatabaseName); // Apparently the library still sets a default database name
    }
}