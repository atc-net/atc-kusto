namespace Atc.Kusto.Tests;

public sealed class KustoProcessorTests
{
    [Theory, AutoNSubstituteData]
    public void Can_Specify_Connection_And_Database(
        IScriptHandlerFactory factory,
        string connectionName,
        string databaseName)
    {
        var sut = new KustoProcessor(
            factory,
            connectionName,
            databaseName);

        sut.ConnectionName.Should().Be(connectionName);
        sut.DatabaseName.Should().Be(databaseName);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteCommand_ShouldCallScriptHandlerExecute(
        [Frozen] IScriptHandlerFactory factory,
        IKustoCommand command,
        IScriptHandler scriptHandler,
        KustoProcessor sut,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(command, sut.ConnectionName, sut.DatabaseName)
            .Returns(scriptHandler);

        // Act
        await sut.ExecuteCommand(command, cancellationToken);

        // Assert
        factory
            .Received(1)
            .Create(
                command,
                sut.ConnectionName,
                sut.DatabaseName);

        await scriptHandler
            .Received(1)
            .Execute(cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteQuery_ShouldCallScriptHandlerExecute(
        [Frozen] IScriptHandlerFactory factory,
        IKustoQuery<TestRecord> query,
        IScriptHandler<TestRecord> scriptHandler,
        KustoProcessor sut,
        TestRecord expectedResult,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(query, sut.ConnectionName, sut.DatabaseName)
            .Returns(scriptHandler);

        scriptHandler
            .Execute(cancellationToken)
            .Returns(expectedResult);

        // Act
        var actualResult = await sut.ExecuteQuery(query, cancellationToken);

        // Assert
        actualResult
            .Should()
            .Be(expectedResult);

        factory
            .Received(1)
            .Create(
                query,
                sut.ConnectionName,
                sut.DatabaseName);

        await scriptHandler
            .Received(1)
            .Execute(cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecutePagedQuery_ShouldCallScriptHandlerExecute(
        [Frozen] IScriptHandlerFactory factory,
        IKustoQuery<IReadOnlyList<TestRecord>> query,
        IScriptHandler<PagedResult<TestRecord>> scriptHandler,
        KustoProcessor sut,
        string sessionId,
        int pageSize,
        string continuationToken,
        PagedResult<TestRecord> expectedResult,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(
                query,
                sessionId,
                pageSize,
                continuationToken,
                sut.ConnectionName,
                sut.DatabaseName)
            .Returns(scriptHandler);

        scriptHandler
            .Execute(cancellationToken)
            .Returns(expectedResult);

        // Act
        var actualResult = await sut.ExecutePagedQuery(
            query,
            sessionId,
            pageSize,
            continuationToken,
            cancellationToken);

        // Assert
        actualResult
            .Should()
            .Be(expectedResult);

        factory
            .Received(1)
            .Create(
                query,
                sessionId,
                pageSize,
                continuationToken,
                sut.ConnectionName,
                sut.DatabaseName);

        await scriptHandler
            .Received(1)
            .Execute(cancellationToken);
    }
}