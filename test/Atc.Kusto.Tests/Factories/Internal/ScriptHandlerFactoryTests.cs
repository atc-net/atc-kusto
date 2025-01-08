namespace Atc.Kusto.Tests.Factories.Internal;

public sealed class ScriptHandlerFactoryTests
{
    [Theory, AutoNSubstituteData]
    internal void Create_Command_ShouldReturnSimpleCommandHandler(
        ScriptHandlerFactory sut,
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
        ScriptHandlerFactory sut,
        IKustoCommand command)
        => sut.Create(command)
            .Should().NotBeNull()
            .And.BeAssignableTo<SimpleCommandHandler>();

    [Theory, AutoNSubstituteData]
    internal void Create_Query_ShouldReturnSimpleQueryHandler(
        ScriptHandlerFactory sut,
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
        ScriptHandlerFactory sut,
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
        ScriptHandlerFactory sut,
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