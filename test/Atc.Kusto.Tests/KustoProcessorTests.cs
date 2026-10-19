namespace Atc.Kusto.Tests;

public sealed class KustoProcessorTests
{
    [Theory, AutoNSubstituteData]
    public void Ctor_Sets_ConnectionName_And_DatabaseName(
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
        var actualResult = await sut.ExecuteQuery(query, cancellationToken: cancellationToken);

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

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteQuery_Can_Be_Called_With_Only_The_Query(
        [Frozen] IScriptHandlerFactory factory,
        IKustoQuery<TestRecord> query,
        IScriptHandler<TestRecord> scriptHandler,
        KustoProcessor sut,
        TestRecord expectedResult)
    {
        // Arrange
        factory
            .Create(query, sut.ConnectionName, sut.DatabaseName)
            .Returns(scriptHandler);

        scriptHandler
            .Execute(Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var actualResult = await sut.ExecuteQuery(query, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        actualResult.Should().Be(expectedResult);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteBufferedStreamingQuery_Can_Be_Called_With_Only_The_Query(
        [Frozen] IScriptHandlerFactory factory,
        IKustoStreamingQuery<TestRecord> query,
        IScriptHandler<StreamingQueryResult<TestRecord>?> scriptHandler,
        KustoProcessor sut,
        StreamingQueryResult<TestRecord> expectedResult)
    {
        // Arrange
        factory
            .CreateBuffered(
                Arg.Is(query),
                Arg.Is(sut.ConnectionName),
                Arg.Is(sut.DatabaseName),
                Arg.Any<AtcStreamingQueryOptions?>())
            .Returns(scriptHandler);

        scriptHandler
            .Execute(Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var actualResult = await sut.ExecuteBufferedStreamingQuery(query, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        actualResult.Should().Be(expectedResult);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteStreamingQuery_Can_Be_Called_With_Only_The_Query(
        [Frozen] IScriptHandlerFactory factory,
        IKustoStreamingQuery<TestRecord> query,
        IStreamingScriptHandler<TestRecord?> scriptHandler,
        KustoProcessor sut,
        TestRecord expectedRecord)
    {
        // Arrange
        factory
            .Create(
                query,
                sut.ConnectionName,
                sut.DatabaseName,
                Arg.Any<AtcStreamingQueryOptions?>())
            .Returns(scriptHandler);

        scriptHandler
            .Execute(Arg.Any<CancellationToken>())
            .Returns(new List<TestRecord?> { expectedRecord }.ToAsyncEnumerable());

        // Act
        var results = new List<TestRecord>();
        await foreach (var item in sut.ExecuteStreamingQuery(query, cancellationToken: TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().Equal(expectedRecord);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteStreamingQuery_ShouldCallStreamingScriptHandlerExecute(
        [Frozen] IScriptHandlerFactory factory,
        IKustoStreamingQuery<TestRecord> query,
        IStreamingScriptHandler<TestRecord?> scriptHandler,
        KustoProcessor sut,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .Create(
                query,
                sut.ConnectionName,
                sut.DatabaseName,
                Arg.Any<AtcStreamingQueryOptions?>())
            .Returns(scriptHandler);

        var testRecords = new List<TestRecord?> { new() };

        scriptHandler
            .Execute(cancellationToken)
            .Returns(testRecords.ToAsyncEnumerable(cancellationToken));

        // Act
        var results = new List<TestRecord>();
        await foreach (var item in sut.ExecuteStreamingQuery(query, cancellationToken))
        {
            results.Add(item);
        }

        // Assert
        Assert.NotEmpty(results);

        factory
            .Received(1)
            .Create(
                Arg.Is(query),
                Arg.Is(sut.ConnectionName),
                Arg.Is(sut.DatabaseName),
                Arg.Any<AtcStreamingQueryOptions?>());

        scriptHandler
            .Received(1)
            .Execute(cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteBufferedStreamingQuery_ShouldCallScriptHandlerExecute(
        [Frozen] IScriptHandlerFactory factory,
        IKustoStreamingQuery<TestRecord> query,
        IScriptHandler<StreamingQueryResult<TestRecord>?> scriptHandler,
        KustoProcessor sut,
        StreamingQueryResult<TestRecord> expectedResult,
        CancellationToken cancellationToken)
    {
        // Arrange
        factory
            .CreateBuffered(
                Arg.Is(query),
                Arg.Is(sut.ConnectionName),
                Arg.Is(sut.DatabaseName),
                Arg.Any<AtcStreamingQueryOptions?>())
            .Returns(scriptHandler);

        scriptHandler
            .Execute(Arg.Is(cancellationToken))
            .Returns(expectedResult);

        // Act
        var actualResult = await sut.ExecuteBufferedStreamingQuery(
            query,
            cancellationToken: cancellationToken);

        // Assert
        actualResult
            .Should()
            .Be(expectedResult);

        factory
            .Received(1)
            .CreateBuffered(
                Arg.Is(query),
                Arg.Is(sut.ConnectionName),
                Arg.Is(sut.DatabaseName),
                Arg.Any<AtcStreamingQueryOptions?>());

        await scriptHandler
            .Received(1)
            .Execute(cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteStreamingQuery_Translates_Cancellation_When_The_Token_Is_Given_Through_WithCancellation(
        [Frozen] IScriptHandlerFactory factory,
        IKustoStreamingQuery<TestRecord> query,
        IStreamingScriptHandler<TestRecord?> scriptHandler,
        KustoProcessor sut)
    {
        // Arrange - ASP.NET Core passes RequestAborted this way when an endpoint returns the stream
        using var cts = new CancellationTokenSource();
        var sdkError = new KustoServiceException();

        factory
            .Create(
                query,
                sut.ConnectionName,
                sut.DatabaseName,
                Arg.Any<AtcStreamingQueryOptions?>())
            .Returns(scriptHandler);

        scriptHandler
            .Execute(Arg.Any<CancellationToken>())
            .Returns(new FailingAsyncEnumerable<TestRecord?>(FailingStep.MoveNext, sdkError, cancelBeforeFailing: cts));

        // Act
        var act = async () =>
        {
            await foreach (var row in sut.ExecuteStreamingQuery(query, CancellationToken.None).WithCancellation(cts.Token))
            {
                _ = row;
            }
        };

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.InnerException.Should().BeSameAs(sdkError);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteCommand_Throws_OperationCanceled_Before_Creating_A_Handler_When_Cancelled(
        [Frozen] IScriptHandlerFactory factory,
        IKustoCommand command,
        KustoProcessor sut)
    {
        // Arrange
        using var cts = CreateCancelledTokenSource();

        // Act
        var act = () => sut.ExecuteCommand(command, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteQuery_Throws_OperationCanceled_Before_Creating_A_Handler_When_Cancelled(
        [Frozen] IScriptHandlerFactory factory,
        IKustoQuery<TestRecord> query,
        KustoProcessor sut)
    {
        // Arrange
        using var cts = CreateCancelledTokenSource();

        // Act
        var act = () => sut.ExecuteQuery(query, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecutePagedQuery_Throws_OperationCanceled_Before_Creating_A_Handler_When_Cancelled(
        [Frozen] IScriptHandlerFactory factory,
        IKustoQuery<IReadOnlyList<TestRecord>> query,
        KustoProcessor sut,
        string sessionId,
        int pageSize,
        string continuationToken)
    {
        // Arrange
        using var cts = CreateCancelledTokenSource();

        // Act
        var act = () => sut.ExecutePagedQuery(query, sessionId, pageSize, continuationToken, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecutePagedQuery_Passes_The_Options_To_The_Factory(
        [Frozen] IScriptHandlerFactory factory,
        IKustoQuery<IReadOnlyList<TestRecord>> query,
        KustoProcessor sut)
    {
        // Arrange
        var options = new AtcPagedQueryOptions
        {
            IncludeTotalCount = true,
        };

        // Act
        await sut.ExecutePagedQuery(query, sessionId: null, pageSize: 10, continuationToken: null, options, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        factory
            .Received(1)
            .Create(
                query,
                sessionId: null,
                pageSize: 10,
                continuationToken: null,
                sut.ConnectionName,
                sut.DatabaseName,
                options);
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecutePagedQuery_Without_A_Page_Size_Returns_The_Item_Count_As_Total_Count_When_Asked(
        [Frozen] IScriptHandlerFactory factory,
        IKustoQuery<IReadOnlyList<TestRecord>> query,
        IScriptHandler<IReadOnlyList<TestRecord>> scriptHandler,
        KustoProcessor sut,
        List<TestRecord> items)
    {
        // Arrange - without a page size, the query runs as a plain query
        factory
            .Create(query, sut.ConnectionName, sut.DatabaseName, Arg.Any<AtcQueryOptions?>())
            .Returns(scriptHandler);

        scriptHandler
            .Execute(Arg.Any<CancellationToken>())
            .Returns(items);

        var options = new AtcPagedQueryOptions
        {
            IncludeTotalCount = true,
        };

        // Act
        var result = await sut.ExecutePagedQuery(query, sessionId: null, pageSize: null, continuationToken: null, options, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result!.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(items.Count);
        result.ContinuationToken.Should().BeNull();
    }

    [Theory]
    [InlineAutoNSubstituteData(0)]
    [InlineAutoNSubstituteData(-1)]
    internal async Task ExecutePagedQuery_Throws_For_A_Page_Size_Of_Zero_Or_Less(
        int pageSize,
        [Frozen] IScriptHandlerFactory factory,
        IKustoQuery<IReadOnlyList<TestRecord>> query,
        KustoProcessor sut)
    {
        // Act
        var act = () => sut.ExecutePagedQuery(query, sessionId: null, pageSize, continuationToken: null);

        // Assert
        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>())
            .Which.ParamName.Should().Be("pageSize");
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteBufferedStreamingQuery_Throws_OperationCanceled_Before_Creating_A_Handler_When_Cancelled(
        [Frozen] IScriptHandlerFactory factory,
        IKustoStreamingQuery<TestRecord> query,
        KustoProcessor sut)
    {
        // Arrange
        using var cts = CreateCancelledTokenSource();

        // Act
        var act = () => sut.ExecuteBufferedStreamingQuery(query, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteStreamingQuery_Throws_OperationCanceled_Before_Creating_A_Handler_When_Cancelled(
        [Frozen] IScriptHandlerFactory factory,
        IKustoStreamingQuery<TestRecord> query,
        KustoProcessor sut)
    {
        // Arrange
        using var cts = CreateCancelledTokenSource();

        // Act
        var act = () => ReadAll(sut.ExecuteStreamingQuery(query, cts.Token));

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, AutoNSubstituteData]
    internal async Task ExecuteStreamingQuery_Passes_A_Setup_Error_Through_When_Not_Cancelled(
        [Frozen] IScriptHandlerFactory factory,
        IKustoStreamingQuery<TestRecord> query,
        KustoProcessor sut)
    {
        // Arrange - e.g. no database configured for the connection
        var setupError = new InvalidOperationException("No database configured");

        factory
            .Create(
                query,
                sut.ConnectionName,
                sut.DatabaseName,
                Arg.Any<AtcStreamingQueryOptions?>())
            .Returns(_ => throw setupError);

        // Act
        var act = () => ReadAll(sut.ExecuteStreamingQuery(query, CancellationToken.None));

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Should().BeSameAs(setupError);
    }

    private static CancellationTokenSource CreateCancelledTokenSource()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts;
    }

    private static async Task ReadAll<T>(IAsyncEnumerable<T> rows)
    {
        await foreach (var row in rows)
        {
            _ = row;
        }
    }
}