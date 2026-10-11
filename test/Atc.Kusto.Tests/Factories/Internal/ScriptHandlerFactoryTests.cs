namespace Atc.Kusto.Tests.Factories.Internal;

public sealed class ScriptHandlerFactoryTests
{
    private readonly IKustoClientProvider clientProvider = Substitute.For<IKustoClientProvider>();
    private readonly IOptionsMonitor<AtcKustoOptions> optionsMonitor = Substitute.For<IOptionsMonitor<AtcKustoOptions>>();
    private readonly ScriptHandlerFactory sut;

    public ScriptHandlerFactoryTests()
    {
        optionsMonitor
            .Get(Arg.Any<string?>())
            .Returns(new AtcKustoOptions());

        sut = new ScriptHandlerFactory(
            NullLoggerFactory.Instance,
            ResiliencePipeline.Empty,
            Substitute.For<IQueryIdProvider>(),
            clientProvider,
            optionsMonitor);
    }

    [Theory, AutoNSubstituteData]
    internal void Create_Command_ShouldReturnSimpleCommandHandler(
        IKustoCommand command)
    {
        // Act
        var actual = sut.Create(command);

        // Assert
        Assert.NotNull(actual);
        Assert.IsType<SimpleCommandHandler>(actual);
    }

    [Theory, AutoNSubstituteData]
    internal void Create_Query_ShouldReturnSimpleQueryHandler(
        IKustoQuery<int> query)
    {
        // Act
        var actual = sut.Create(query);

        // Assert
        Assert.NotNull(actual);
        Assert.IsType<SimpleQueryHandler<int>>(actual);
    }

    [Theory, AutoNSubstituteData]
    internal void Create_PagedResult_ShouldReturnNewPagedStoredQueryHandler_WhenContinuationTokenIsNull(
        IKustoQuery<IReadOnlyList<int>> query,
        string sessionId,
        int pageSize)
    {
        // Act
        var actual = sut.Create(query, sessionId, pageSize, continuationToken: null);

        // Assert
        Assert.NotNull(actual);
        Assert.IsType<NewPagedStoredQueryHandler<int>>(actual);
    }

    [Theory, AutoNSubstituteData]
    internal void Create_PagedResult_ShouldReturnExistingPagedStoredQueryHandler_WhenContinuationTokenIsNotNull(
        IKustoQuery<IReadOnlyList<int>> query,
        string sessionId,
        int pageSize,
        string continuationToken)
    {
        // Act
        var actual = sut.Create(query, sessionId, pageSize, continuationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.IsType<ExistingPagedStoredQueryHandler<int>>(actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(-1)]
    [InlineData((24 * 60 * 60) + 1)]
    public void Create_PagedResult_Throws_When_The_PagedResultExpiration_Is_Not_Between_1_Second_And_24_Hours(
        double expirationSeconds)
    {
        // Arrange
        optionsMonitor
            .Get(Arg.Any<string?>())
            .Returns(new AtcKustoOptions { PagedResultExpiration = TimeSpan.FromSeconds(expirationSeconds) });

        var query = Substitute.For<IKustoQuery<IReadOnlyList<int>>>();

        // Act
        var act = () => sut.Create(query, sessionId: null, pageSize: 10, continuationToken: null);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*PagedResultExpiration*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24 * 60 * 60)]
    public void Create_PagedResult_Accepts_A_PagedResultExpiration_Of_1_Second_Or_24_Hours(
        double expirationSeconds)
    {
        // Arrange
        optionsMonitor
            .Get(Arg.Any<string?>())
            .Returns(new AtcKustoOptions { PagedResultExpiration = TimeSpan.FromSeconds(expirationSeconds) });

        var query = Substitute.For<IKustoQuery<IReadOnlyList<int>>>();

        // Act
        var act = () => sut.Create(query, sessionId: null, pageSize: 10, continuationToken: null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Create_PagedResult_Uses_The_PagedResultExpiration_Of_The_Connection()
    {
        // Arrange
        var adminProvider = Substitute.For<ICslAdminProvider>();
        clientProvider
            .GetAdminClient("sales", null)
            .Returns(adminProvider);

        optionsMonitor
            .Get("sales")
            .Returns(new AtcKustoOptions { PagedResultExpiration = TimeSpan.FromMinutes(90) });

        var query = Substitute.For<IKustoQuery<IReadOnlyList<int>>>();
        query.GetQueryText().Returns("Sales");

        var handler = sut.Create(
            query,
            sessionId: null,
            pageSize: 10,
            continuationToken: null,
            connectionName: "sales");

        // Act
        await handler.Execute(CancellationToken.None);

        // Assert
        await adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Is<string>(command => command.Contains("expiresAfter = 5400s", StringComparison.Ordinal)),
                Arg.Any<ClientRequestProperties>());
    }

    [Fact]
    public async Task Create_Query_Passes_The_SerializerOptions_Of_The_Connection_To_ReadResult()
    {
        // Arrange
        var connectionOptions = new AtcKustoOptions();
        optionsMonitor
            .Get("sales")
            .Returns(connectionOptions);

        var reader = Substitute.For<IDataReader>();
        var queryProvider = Substitute.For<ICslQueryProvider>();

        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(reader);

        clientProvider
            .GetQueryClient("sales", null)
            .Returns(queryProvider);

        var query = Substitute.For<IKustoQuery<int>>();
        var handler = sut.Create(query, connectionName: "sales");

        // Act
        await handler.Execute(CancellationToken.None);

        // Assert
        query
            .Received(1)
            .ReadResult(reader, connectionOptions.SerializerOptions);
    }
}