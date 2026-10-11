namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class NewPagedStoredQueryHandlerTests
{
    private static readonly TimeSpan Expiration = TimeSpan.FromHours(1);

    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslAdminProvider adminProvider;
    private readonly IKustoQuery<IReadOnlyList<string>> query;
    private readonly int pageSize;
    private readonly string sessionId;
    private readonly string queryId;
    private readonly NewPagedStoredQueryHandler<string> sut;

    public NewPagedStoredQueryHandlerTests()
    {
        queryIdProvider = Substitute.For<IQueryIdProvider>();
        adminProvider = Substitute.For<ICslAdminProvider>();
        query = Substitute.For<IKustoQuery<IReadOnlyList<string>>>();

        var fixture = FixtureFactory.Create();
        pageSize = 3;
        sessionId = fixture.Create<string>().ToAlphanumeric();
        queryId = fixture.Create<string>().ToAlphanumeric();

        queryIdProvider
            .Create(query, sessionId, nonce: null)
            .ReturnsForAnyArgs(queryId);

        sut = new NewPagedStoredQueryHandler<string>(ResiliencePipeline.Empty, queryIdProvider, adminProvider, query, sessionId, pageSize, Expiration, includeTotalCount: false);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnPagedResult_WhenQueryExecutesSuccessfully(
        [Frozen] IDataReader reader,
        string queryText,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // Arrange - a full page, so there is a next page
        IReadOnlyList<string> items = ["A", "B", "C"];
        query.GetQueryText().Returns(queryText);
        query.GetParameters().Returns(parameters);

        query
            .ReadResult(reader)
            .ReturnsForAnyArgs(items);

        adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties())
            .ReturnsForAnyArgs(reader);

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        result.Should().NotBeNull();

        result!.Items.Should().BeEquivalentTo(items);
        result.ContinuationToken.Should().Be("v2;;3");
        result.TotalCount.Should().BeNull();

        queryIdProvider
            .Received(1)
            .Create(query, sessionId, nonce: null);

        _ = query
            .Received(1)
            .ReadResult(reader);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                databaseName: null,
                $"""
                  .set-or-replace stored_query_result ['{queryId}'] with (previewCount = {pageSize}, expiresAfter = 3600s) <|
                  {queryText}
                  | serialize row_number = row_number()
                  """.Replace("\r\n", "\n", StringComparison.Ordinal),
                Arg.Is<ClientRequestProperties>(p => p.ClientRequestId != null));
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnNoContinuationToken_WhenThePageIsNotFull(
        IDataReader reader)
    {
        // Arrange
        query
            .ReadResult(reader)
            .Returns(["A", "B"]);

        adminProvider
            .ExecuteControlCommandAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ClientRequestProperties>())
            .Returns(reader);

        // Act
        var result = await sut.Execute(CancellationToken.None);

        // Assert
        result!.Items.Should().Equal("A", "B");
        result.ContinuationToken.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnNull_WhenQueryReturnsNull(
        [Frozen] IDataReader reader,
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);

        query
            .ReadResult(reader)
            .ReturnsForAnyArgs((IReadOnlyList<string>?)null);

        adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties())
            .ReturnsForAnyArgs(reader);

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        result.Should().BeNull();

        queryIdProvider
            .Received(1)
            .Create(query, sessionId, nonce: null);

        _ = query
            .Received(1)
            .ReadResult(reader);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                databaseName: null,
                $"""
                 .set-or-replace stored_query_result ['{queryId}'] with (previewCount = {pageSize}, expiresAfter = 3600s) <|
                 {queryText}
                 | serialize row_number = row_number()
                 """.Replace("\r\n", "\n", StringComparison.Ordinal),
                Arg.Is<ClientRequestProperties>(p => p.ClientRequestId != null));
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Writes_A_Random_Nonce_In_The_Token_Instead_Of_The_QueryId_When_There_Is_No_Session(
        IDataReader reader)
    {
        // Arrange - a full first page, so there is a next page
        string? usedNonce = null;
        queryIdProvider
            .Create(query, Arg.Is<string?>(sessionId => sessionId == null), Arg.Do<string?>(nonce => usedNonce = nonce))
            .Returns(queryId);

        adminProvider
            .ExecuteControlCommandAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ClientRequestProperties>())
            .Returns(reader);

        query
            .ReadResult(reader)
            .Returns(["A", "B", "C"]);

        var handler = new NewPagedStoredQueryHandler<string>(ResiliencePipeline.Empty, queryIdProvider, adminProvider, query, sessionId: null, pageSize, Expiration, includeTotalCount: false);

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert - the token carries the nonce, never the queryId
        usedNonce.Should().MatchRegex("^[0-9a-f]{32}$");
        result!.ContinuationToken.Should().Be($"v2;{usedNonce};3");
        result.ContinuationToken.Should().NotContain(queryId);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Creates_A_New_Random_Nonce_Each_Time_It_Runs_Without_A_Session(
        IDataReader reader)
    {
        // Arrange
        var usedNonces = new List<string?>();
        queryIdProvider
            .Create(query, Arg.Is<string?>(sessionId => sessionId == null), Arg.Do<string?>(usedNonces.Add))
            .Returns(queryId);

        adminProvider
            .ExecuteControlCommandAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ClientRequestProperties>())
            .Returns(reader);

        var handler = new NewPagedStoredQueryHandler<string>(ResiliencePipeline.Empty, queryIdProvider, adminProvider, query, sessionId: null, pageSize, Expiration, includeTotalCount: false);

        // Act
        await handler.Execute(CancellationToken.None);
        await handler.Execute(CancellationToken.None);

        // Assert
        usedNonces.Should().HaveCount(2);
        usedNonces[1].Should().NotBe(usedNonces[0]);
    }

    [Fact]
    public async Task Execute_Does_Not_Create_The_Stored_Result_When_Already_Cancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => sut.Execute(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        await adminProvider
            .DidNotReceive()
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Retries_A_Temporary_Error_And_Returns_The_First_Page(
        IDataReader reader)
    {
        // Arrange - the first attempt times out, the second one succeeds
        adminProvider
            .ExecuteControlCommandAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ClientRequestProperties>())
            .Returns(
                _ => Task.FromException<IDataReader>(new TimeoutException()),
                _ => Task.FromResult(reader));

        query
            .ReadResult(reader)
            .Returns(["A", "B"]);

        var handler = new NewPagedStoredQueryHandler<string>(
            CreateRetryOnTimeoutPipeline(),
            queryIdProvider,
            adminProvider,
            query,
            sessionId,
            pageSize,
            Expiration,
            includeTotalCount: false);

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert
        result!.Items.Should().Equal("A", "B");

        await adminProvider
            .Received(2)
            .ExecuteControlCommandAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ClientRequestProperties>());
    }

    [Fact]
    public Task Execute_Throws_An_Error_That_Is_Not_Temporary()
    {
        // Arrange
        adminProvider
            .ExecuteControlCommandAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ClientRequestProperties>())
            .ThrowsAsync(new SemanticException());

        // Act
        var act = () => sut.Execute(CancellationToken.None);

        // Assert
        return act.Should().ThrowAsync<SemanticException>();
    }

    [Fact]
    public async Task Execute_Does_Not_Retry_Once_The_Caller_Has_Cancelled()
    {
        // Arrange - the caller cancels while the first attempt times out
        using var cts = new CancellationTokenSource();

        adminProvider
            .ExecuteControlCommandAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ClientRequestProperties>())
            .Returns(_ => CancellationTestData.CancelThenThrow<IDataReader>(cts.CancelAsync, new TimeoutException()));

        var handler = new NewPagedStoredQueryHandler<string>(
            CreateRetryOnTimeoutPipeline(),
            queryIdProvider,
            adminProvider,
            query,
            sessionId,
            pageSize,
            Expiration,
            includeTotalCount: false);

        // Act
        var act = () => handler.Execute(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        await adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ClientRequestProperties>());
    }

    [Theory]
    [MemberData(nameof(CancellationTestData.SdkCancellationErrors), MemberType = typeof(CancellationTestData))]
    public async Task Execute_Throws_OperationCanceledException_When_The_Sdk_Fails_After_The_Caller_Cancelled(
        Exception sdkError)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>())
            .Returns(_ => CancellationTestData.CancelThenThrow<IDataReader>(cts.CancelAsync, sdkError));

        // Act
        var act = () => sut.Execute(cts.Token);

        // Assert
        var thrown = await act.Should().ThrowAsync<OperationCanceledException>();
        thrown.Which.InnerException.Should().BeSameAs(sdkError);
        thrown.Which.CancellationToken.Should().Be(cts.Token);
    }

    // Retries a timeout once, without waiting, like the real pipeline retries a temporary error.
    private static ResiliencePipeline CreateRetryOnTimeoutPipeline()
        => new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                MaxRetryAttempts = 1,
                Delay = TimeSpan.Zero,
            })
            .Build();

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Reads_The_Total_Count_From_The_Stored_Result_When_The_First_Page_Is_Full(
        IDataReader pageReader)
    {
        // Arrange
        using var showReader = CreateShowStoredQueryResultReader(rowCount: 7);
        ReturnFromControlCommands(pageReader, showReader);

        query
            .ReadResult(pageReader)
            .Returns(["A", "B", "C"]);

        var handler = CreateHandlerWithTotalCount();

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert
        result!.TotalCount.Should().Be(7);
        result.ContinuationToken.Should().Be("v2;;3;7");

        await adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                databaseName: null,
                $".show stored_query_results ['{queryId}']",
                Arg.Any<ClientRequestProperties>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Uses_The_Item_Count_As_Total_Count_When_The_First_Page_Is_Not_Full(
        IDataReader pageReader)
    {
        // Arrange
        using var showReader = CreateShowStoredQueryResultReader(rowCount: 99);
        ReturnFromControlCommands(pageReader, showReader);

        query
            .ReadResult(pageReader)
            .Returns(["A", "B"]);

        var handler = CreateHandlerWithTotalCount();

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert - no metadata call is needed
        result!.TotalCount.Should().Be(2);
        result.ContinuationToken.Should().BeNull();

        await adminProvider
            .DidNotReceive()
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Is<string>(command => command.StartsWith(".show", StringComparison.Ordinal)),
                Arg.Any<ClientRequestProperties>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Returns_No_Continuation_Token_When_The_First_Page_Holds_The_Total_Count(
        IDataReader pageReader)
    {
        // Arrange - a full page of 3 that is also all there is
        using var showReader = CreateShowStoredQueryResultReader(rowCount: 3);
        ReturnFromControlCommands(pageReader, showReader);

        query
            .ReadResult(pageReader)
            .Returns(["A", "B", "C"]);

        var handler = CreateHandlerWithTotalCount();

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert
        result!.TotalCount.Should().Be(3);
        result.ContinuationToken.Should().BeNull();
    }

    private NewPagedStoredQueryHandler<string> CreateHandlerWithTotalCount()
        => new(
            ResiliencePipeline.Empty,
            queryIdProvider,
            adminProvider,
            query,
            sessionId,
            pageSize,
            Expiration,
            includeTotalCount: true);

    private void ReturnFromControlCommands(
        IDataReader pageReader,
        IDataReader showReader)
    {
        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Is<string>(command => command.StartsWith(".set-or-replace", StringComparison.Ordinal)),
                Arg.Any<ClientRequestProperties>())
            .Returns(pageReader);

        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Is<string>(command => command.StartsWith(".show", StringComparison.Ordinal)),
                Arg.Any<ClientRequestProperties>())
            .Returns(showReader);
    }

    // The result of ".show stored_query_results", reduced to the column that is read.
    private static DataTableReader CreateShowStoredQueryResultReader(
        long rowCount)
    {
        using var table = new DataTable();
        table.Columns.Add("RowCount", typeof(long));
        table.Rows.Add(rowCount);

        return table.CreateDataReader();
    }
}