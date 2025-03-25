namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class ExistingPagedStoredQueryHandlerTests
{
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<IReadOnlyList<string>> query;
    private readonly int pageSize;
    private readonly string queryId;
    private readonly int itemsReturned;
    private readonly ExistingPagedStoredQueryHandler<string> sut;

    public ExistingPagedStoredQueryHandlerTests()
    {
        queryProvider = Substitute.For<ICslQueryProvider>();
        query = Substitute.For<IKustoQuery<IReadOnlyList<string>>>();

        var fixture = FixtureFactory.Create();
        pageSize = 3;
        queryId = fixture.Create<string>().ToAlphanumeric();
        itemsReturned = fixture.Create<int>();
        var continuationToken = $"{queryId};{itemsReturned}";

        sut = new ExistingPagedStoredQueryHandler<string>(
            new NullLogger<ExistingPagedStoredQueryHandler<string>>(),
            ResiliencePipeline.Empty,
            queryProvider,
            query,
            pageSize,
            continuationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnPagedResult_WhenQueryExecutesSuccessfully(
        [Frozen] IDataReader reader,
        string queryText,
        Dictionary<string, object> parameters,
        List<string> items,
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
            .ReturnsForAnyArgs(items);

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().BeEquivalentTo(items);

        if (items.Count < pageSize)
        {
            result.ContinuationToken.Should().BeNull();
        }
        else
        {
            var expectedContinuationToken = $"{queryId};{itemsReturned + items.Count}";
            result.ContinuationToken.Should().Be(expectedContinuationToken);
        }

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
            .ReadResult(reader);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnNull_WhenContinuationTokenIsInvalid(
        string invalidContinuationToken,
        CancellationToken cancellationToken)
    {
        // Arrange
        var handler = new ExistingPagedStoredQueryHandler<string>(
            new NullLogger<ExistingPagedStoredQueryHandler<string>>(),
            ResiliencePipeline.Empty,
            queryProvider,
            query,
            pageSize,
            invalidContinuationToken);

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
            .ReadResult(reader)
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
            .ReadResult(reader);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnNull_WhenSemanticExceptionIsThrown(
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
            .ReadResult(Arg.Any<IDataReader>());
    }
}