namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class SimpleQueryHandlerTests
{
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<string> query;

    private readonly SimpleQueryHandler<string> sut;

    public SimpleQueryHandlerTests()
    {
        queryProvider = Substitute.For<ICslQueryProvider>();
        query = Substitute.For<IKustoQuery<string>>();

        sut = new SimpleQueryHandler<string>(
            new NullLogger<SimpleQueryHandler<string>>(),
            ResiliencePipeline.Empty,
            queryProvider,
            query,
            new AtcQueryOptions { EnableServerSideCancellation = false });
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldIssueCancelCommand_WhenTokenCanceled(
        ICslAdminProvider adminProvider,
        IDataReader cancelReader)
    {
        // Arrange
        var logger = new NullLogger<SimpleQueryHandler<string>>();
        var options = new AtcQueryOptions { EnableServerSideCancellation = true };

        query.GetQueryText().Returns("print 1");

        ClientRequestProperties? capturedProps = null;

        // The cancel command is sent from a background task; signal when it arrives instead of sleeping.
        var cancelCommandSent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>())
            .Returns(ci =>
            {
                cancelCommandSent.TrySetResult(ci.ArgAt<string>(1));
                return cancelReader;
            });

        // Simulate a long-running query that respects cancellation
        queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedProps = ci.Arg<ClientRequestProperties>();
                var ct = ci.Arg<CancellationToken>();
                var tcs = new TaskCompletionSource<IDataReader>();
                ct.Register(() => tcs.TrySetCanceled(ct));
                return tcs.Task;
            });

        var handler = new SimpleQueryHandler<string>(
            logger,
            ResiliencePipeline.Empty,
            adminProvider,
            queryProvider,
            query,
            options);

        using var cts = new CancellationTokenSource();

        var execTask = handler.Execute(cts.Token);

        // Act - cancel the token to trigger server-side cancel
        await cts.CancelAsync();

        var act = async () => await execTask;
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Not cts.Token: it is already cancelled, and this waits for the effect of cancelling it.
        var cancelCommand = await cancelCommandSent.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            CancellationToken.None);

        // Assert - a cancel control command was issued with the original ClientRequestId
        capturedProps.Should().NotBeNull();
        capturedProps!.ClientRequestId.Should().NotBeNullOrEmpty();
        cancelCommand.Should().ContainEquivalentOf("cancel");
        cancelCommand.Should().Contain(capturedProps.ClientRequestId);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldNotIssueCancelCommand_WhenServerSideCancelDisabled(
        ICslAdminProvider adminProvider)
    {
        // Arrange
        var logger = new NullLogger<SimpleQueryHandler<string>>();
        var options = new AtcQueryOptions { EnableServerSideCancellation = false };

        query.GetQueryText().Returns("print 1");

        // Simulate a long-running query that respects cancellation
        queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ct = ci.Arg<CancellationToken>();
                var tcs = new TaskCompletionSource<IDataReader>();
                ct.Register(() => tcs.TrySetCanceled(ct));
                return tcs.Task;
            });

        var handler = new SimpleQueryHandler<string>(
            logger,
            ResiliencePipeline.Empty,
            adminProvider,
            queryProvider,
            query,
            options);

        using var cts = new CancellationTokenSource();

        var execTask = handler.Execute(cts.Token);

        // Act - cancel the token
        await cts.CancelAsync();

        var act = async () => await execTask;
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Assert - no cancel command was sent. No wait is needed: with server-side cancellation
        // disabled no cancellation callback is registered, so no background task can send one later.
        await adminProvider
            .DidNotReceive()
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnResult_WhenQueryExecutesSuccessfully(
        [Frozen] IDataReader reader,
        string queryText,
        Dictionary<string, object> parameters,
        string expectedResult,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);
        query.GetParameters().Returns(parameters);

        queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties(),
                cancellationToken)
            .ReturnsForAnyArgs(reader);

        query
            .ReadResult(reader)
            .ReturnsForAnyArgs(expectedResult);

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        result
            .Should()
            .Be(expectedResult);

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                Arg.Is<ClientRequestProperties>(p
                        => p.ClientRequestId != null &&
                           p.Parameters.SequenceEqual(query.GetCslParameters())),
                cancellationToken);

        query
            .Received(1)
            .ReadResult(reader);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnNull_WhenQueryResultIsNull(
        [Frozen] IDataReader reader,
        CancellationToken cancellationToken)
    {
        // Arrange
        queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties(),
                cancellationToken)
            .ReturnsForAnyArgs(reader);

        query
            .ReadResult(reader)
            .ReturnsForAnyArgs((string?)null);

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        Assert.Null(result);

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null &&
                       p.Parameters.SequenceEqual(query.GetCslParameters())),
                cancellationToken);

        query
            .Received(1)
            .ReadResult(reader);
    }

    [Theory]
    [MemberData(nameof(CancellationTestData.SdkCancellationErrors), MemberType = typeof(CancellationTestData))]
    public async Task Execute_Throws_OperationCanceledException_When_The_Sdk_Fails_After_The_Caller_Cancelled(
        Exception sdkError)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CancellationTestData.CancelThenThrow<IDataReader>(cts.CancelAsync, sdkError));

        // Act
        var act = () => sut.Execute(cts.Token);

        // Assert
        var thrown = await act.Should().ThrowAsync<OperationCanceledException>();
        thrown.Which.InnerException.Should().BeSameAs(sdkError);
        thrown.Which.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Execute_Returns_Null_For_A_Kusto_Cancel_Error_When_The_Caller_Did_Not_Cancel()
    {
        // Arrange - e.g. someone else ran ".cancel query": a failure, not cancellation by the caller
        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new KustoClientRequestCanceledByUserException());

        // Act
        var result = await sut.Execute(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}