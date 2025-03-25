namespace Atc.Kusto.Tests.Factories.Internal;

public sealed class ScriptHandlerFactoryTests
{
    private readonly ScriptHandlerFactory sut = new(
        NullLoggerFactory.Instance,
        ResiliencePipeline.Empty,
        Substitute.For<IQueryIdProvider>(),
        Substitute.For<IKustoClientProvider>());

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
    internal void Create_Command_ShouldReturnSimpleCommandHandler2(
        IKustoCommand command)
        => sut.Create(command)
            .Should().NotBeNull()
            .And.BeAssignableTo<SimpleCommandHandler>();

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
}