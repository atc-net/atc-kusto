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

    [Fact]
    internal async Task Execute_ShouldIssueCancelCommand_WhenTokenCanceled()
    {
        // Arrange
        var adminProvider = Substitute.For<ICslAdminProvider>();
        var logger = new NullLogger<SimpleQueryHandler<string>>();
        var options = new AtcQueryOptions { EnableServerSideCancellation = true };

        query.GetQueryText().Returns("print 1");

        ClientRequestProperties? capturedProps = null;

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

        try
        {
            await execTask;
        }
        catch
        {
            // Ignored - cancellation may propagate
        }

        // Give the background cancellation task a brief moment to execute
        await Task.Delay(50);

        // Assert - Verify a cancel control command was issued with the original ClientRequestId
        var hasClientRequestId = capturedProps is not null && !string.IsNullOrEmpty(capturedProps.ClientRequestId);

        await adminProvider
            .Received()
            .ExecuteControlCommandAsync(
                databaseName: Arg.Any<string?>(),
                Arg.Is<string>(cmd => cmd.Contains("cancel", StringComparison.OrdinalIgnoreCase)
                    && hasClientRequestId
                    && cmd.Contains(capturedProps!.ClientRequestId!, StringComparison.Ordinal)),
                Arg.Any<ClientRequestProperties>());
    }

    [Fact]
    internal async Task Execute_ShouldNotIssueCancelCommand_WhenServerSideCancelDisabled()
    {
        // Arrange
        var adminProvider = Substitute.For<ICslAdminProvider>();
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

        try
        {
            await execTask;
        }
        catch
        {
            // ignored
        }

        await Task.Delay(50);

        // Assert - Verify no cancel command was sent
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
}