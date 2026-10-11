namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class ExistingPagedStoredQueryHandlerTests
{
    private const string Nonce = "nonce1";

    private static readonly TimeSpan Expiration = TimeSpan.FromHours(1);

    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslAdminProvider adminProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<IReadOnlyList<string>> query;
    private readonly int pageSize;
    private readonly string queryId;
    private readonly int itemsReturned;
    private readonly ExistingPagedStoredQueryHandler<string> sut;

    public ExistingPagedStoredQueryHandlerTests()
    {
        queryIdProvider = Substitute.For<IQueryIdProvider>();
        adminProvider = Substitute.For<ICslAdminProvider>();
        queryProvider = Substitute.For<ICslQueryProvider>();
        query = Substitute.For<IKustoQuery<IReadOnlyList<string>>>();

        var fixture = FixtureFactory.Create();
        pageSize = 3;
        queryId = fixture.Create<string>().ToAlphanumeric();
        itemsReturned = fixture.Create<int>();

        queryIdProvider
            .Create(query, sessionId: null, Nonce)
            .Returns(queryId);

        sut = CreateHandler($"v2;{Nonce};{itemsReturned}");
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

        queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties(),
                cancellationToken)
            .ReturnsForAnyArgs(reader);

        query
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>())
            .ReturnsForAnyArgs(items);

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().BeEquivalentTo(items);
        result.ContinuationToken.Should().Be($"v2;{Nonce};{itemsReturned + 3}");

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                databaseName: null,
                $"stored_query_result('{queryId}') " +
                $"| where row_number between({itemsReturned + 1} .. {itemsReturned + pageSize})",
                Arg.Is<ClientRequestProperties>(p => p.ClientRequestId != null),
                cancellationToken);

        _ = query
            .Received(1)
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnNoContinuationToken_WhenThePageIsNotFull(
        IDataReader reader)
    {
        // Arrange
        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(reader);

        query
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>())
            .Returns(["A", "B"]);

        // Act
        var result = await sut.Execute(CancellationToken.None);

        // Assert
        result!.Items.Should().Equal("A", "B");
        result.ContinuationToken.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnNull_WhenContinuationTokenIsInvalid(
        string invalidContinuationToken,
        CancellationToken cancellationToken)
    {
        // Arrange
        var handler = CreateHandler(invalidContinuationToken);

        // Act
        var result = await handler.Execute(cancellationToken);

        // Assert
        result.Should().BeNull();

        await queryProvider
            .DidNotReceive()
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Returns_Null_For_A_Token_That_Contains_A_QueryId()
    {
        // Arrange - a token in the format "{queryId};{items returned}"
        var handler = CreateHandler($"{queryId};5");

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert
        result.Should().BeNull();

        await queryProvider
            .DidNotReceive()
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Calculates_The_QueryId_Instead_Of_Reading_It_From_The_Token()
    {
        // Arrange - another query's queryId in the nonce slot only becomes an ingredient of the calculated queryId
        queryIdProvider
            .Create(query, sessionId: null, "SomeOtherQuerysQueryId")
            .Returns("CalculatedQueryId");

        var handler = CreateHandler("v2;SomeOtherQuerysQueryId;5");

        // Act
        await handler.Execute(CancellationToken.None);

        // Assert
        await queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                databaseName: null,
                "stored_query_result('CalculatedQueryId') | where row_number between(6 .. 8)",
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnNull_WhenQueryReturnsNull(
        [Frozen] IDataReader reader,
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);

        queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties(),
                cancellationToken)
            .ReturnsForAnyArgs(reader);

        query
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>())
            .ReturnsForAnyArgs((IReadOnlyList<string>?)null);

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        result.Should().BeNull();

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                databaseName: null,
                $"stored_query_result('{queryId}') " +
                $"| where row_number between({itemsReturned + 1} .. {itemsReturned + pageSize})",
                Arg.Is<ClientRequestProperties>(p => p.ClientRequestId != null),
                cancellationToken);

        _ = query
            .Received(1)
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Runs_The_Query_Again_When_The_Stored_Result_Has_Expired(
        IDataReader reader)
    {
        // Arrange - the stored result has expired, so the first read fails and the second one succeeds
        query.GetQueryText().Returns("Devices | order by DeviceId asc");

        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<IDataReader>(new SemanticException()),
                _ => Task.FromResult(reader));

        query
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>())
            .Returns(["A", "B", "C"]);

        var handler = CreateHandlerWithAdminProvider();

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert - the query ran again into the same queryId, and the requested page was read from it
        result!.Items.Should().Equal("A", "B", "C");
        result.ContinuationToken.Should().Be($"v2;{Nonce};{itemsReturned + 3}");

        await adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                databaseName: null,
                $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = 3, expiresAfter = 3600s) <|\n" +
                "Devices | order by DeviceId asc\n" +
                "| serialize row_number = row_number()",
                Arg.Any<ClientRequestProperties>());

        await queryProvider
            .Received(2)
            .ExecuteQueryAsync(
                databaseName: null,
                $"stored_query_result('{queryId}') " +
                $"| where row_number between({itemsReturned + 1} .. {itemsReturned + pageSize})",
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_Returns_Null_When_The_Stored_Result_Is_Still_Missing_After_Running_The_Query_Again()
    {
        // Arrange
        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new SemanticException());

        var handler = CreateHandlerWithAdminProvider();

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert - the query ran again once, not in a loop
        result.Should().BeNull();

        await adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Is<string>(command => command.StartsWith(".set-or-replace", StringComparison.Ordinal)),
                Arg.Any<ClientRequestProperties>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Returns_An_Empty_Page_Past_The_Last_Row_Without_Running_The_Query_Again(
        IDataReader reader)
    {
        // Arrange - e.g. 6 rows with a page size of 3: the token after page 2 points past the last row
        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(reader);

        query
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>())
            .Returns([]);

        var handler = CreateHandlerWithAdminProvider();

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert
        result!.Items.Should().BeEmpty();
        result.ContinuationToken.Should().BeNull();

        await adminProvider
            .DidNotReceive()
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>());
    }

    [Fact]
    public async Task Execute_Does_Not_Run_The_Query_Again_When_The_Caller_Cancelled()
    {
        // Arrange - the caller cancels while the page is read, and the read fails as not found
        using var cts = new CancellationTokenSource();

        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CancellationTestData.CancelThenThrow<IDataReader>(cts.CancelAsync, new SemanticException()));

        var handler = CreateHandlerWithAdminProvider();

        // Act
        var act = () => handler.Execute(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        await adminProvider
            .DidNotReceive()
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Is<string>(command => command.StartsWith(".set-or-replace", StringComparison.Ordinal)),
                Arg.Any<ClientRequestProperties>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Returns_Null_When_The_Stored_Result_Has_Expired_And_There_Is_No_Admin_Provider(
        CancellationToken cancellationToken)
    {
        // Arrange
        queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties(),
                cancellationToken)
            .ThrowsAsyncForAnyArgs(new SemanticException());

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        result.Should().BeNull();

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                databaseName: null,
                $"stored_query_result('{queryId}') " +
                $"| where row_number between({itemsReturned + 1} .. {itemsReturned + pageSize})",
                Arg.Is<ClientRequestProperties>(p => p.ClientRequestId != null),
                cancellationToken);

        _ = query
            .DidNotReceive()
            .ReadResult(Arg.Any<IDataReader>(), Arg.Any<JsonSerializerOptions>());
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

    private ExistingPagedStoredQueryHandler<string> CreateHandler(
        string continuationToken)
        => new(
            new NullLogger<ExistingPagedStoredQueryHandler<string>>(),
            ResiliencePipeline.Empty,
            queryIdProvider,
            queryProvider,
            query,
            KustoJsonSerializerOptions.Default,
            sessionId: null,
            pageSize,
            Expiration,
            continuationToken);

    private ExistingPagedStoredQueryHandler<string> CreateHandlerWithAdminProvider()
        => CreateHandlerWithAdminProvider($"v2;{Nonce};{itemsReturned}");

    private ExistingPagedStoredQueryHandler<string> CreateHandlerWithAdminProvider(
        string continuationToken)
        => new(
            new NullLogger<ExistingPagedStoredQueryHandler<string>>(),
            ResiliencePipeline.Empty,
            queryIdProvider,
            adminProvider,
            queryProvider,
            query,
            KustoJsonSerializerOptions.Default,
            sessionId: null,
            pageSize,
            Expiration,
            continuationToken);

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Carries_The_Total_Count_Of_The_Token_To_The_Next_Page(
        IDataReader reader)
    {
        // Arrange
        ReturnPage(reader, "A", "B", "C");
        var handler = CreateHandler($"v2;{Nonce};{itemsReturned};{itemsReturned + 10}");

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert
        result!.TotalCount.Should().Be(itemsReturned + 10);
        result.ContinuationToken.Should().Be($"v2;{Nonce};{itemsReturned + 3};{itemsReturned + 10}");
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Returns_No_Continuation_Token_When_The_Total_Count_Is_Reached(
        IDataReader reader)
    {
        // Arrange - a full page that reaches the total
        ReturnPage(reader, "A", "B", "C");
        var handler = CreateHandler($"v2;{Nonce};{itemsReturned};{itemsReturned + 3}");

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert
        result!.ContinuationToken.Should().BeNull();
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_Reads_The_Total_Count_Again_After_Running_The_Query_Again(
        IDataReader reader)
    {
        // Arrange - the stored result has expired, and the new run has 50 rows instead of 10
        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<IDataReader>(new SemanticException()),
                _ => Task.FromResult(reader));

        query
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>())
            .Returns(["A", "B", "C"]);

        using var showReader = CreateShowStoredQueryResultReader(rowCount: itemsReturned + 50);
        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string?>(),
                Arg.Is<string>(command => command.StartsWith(".show", StringComparison.Ordinal)),
                Arg.Any<ClientRequestProperties>())
            .Returns(showReader);

        var handler = CreateHandlerWithAdminProvider($"v2;{Nonce};{itemsReturned};{itemsReturned + 10}");

        // Act
        var result = await handler.Execute(CancellationToken.None);

        // Assert
        result!.TotalCount.Should().Be(itemsReturned + 50);
        result.ContinuationToken.Should().Be($"v2;{Nonce};{itemsReturned + 3};{itemsReturned + 50}");
    }

    private void ReturnPage(
        IDataReader reader,
        params string[] items)
    {
        queryProvider
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(reader);

        query
            .ReadResult(reader, Arg.Any<JsonSerializerOptions>())
            .Returns(items);
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