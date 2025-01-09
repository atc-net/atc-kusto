namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class NewPagedStoredQueryHandlerTests
{
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
            .Create(query.GetType(), sessionId)
            .ReturnsForAnyArgs(queryId);

        sut = new NewPagedStoredQueryHandler<string>(queryIdProvider, adminProvider, query, sessionId, pageSize);
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

        queryIdProvider
            .Create(query.GetType(), sessionId)
            .ReturnsForAnyArgs(queryId);

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

        if (items.Count < pageSize)
        {
            result.ContinuationToken.Should().BeNull();
        }
        else
        {
            result.ContinuationToken.Should().Be($"{queryId};{items.Count}");
        }

        queryIdProvider
            .Received(1)
            .Create(query.GetType(), sessionId);

        _ = query
            .Received(1)
            .ReadResult(reader);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                databaseName: null,
                $"""
                  .set-or-replace stored_query_result ['{queryId}'] with (previewCount = {pageSize}, expiresAfter = 1h) <|
                  {queryText}
                  | serialize row_number = row_number()
                  """.Replace("\r\n", "\n", StringComparison.Ordinal),
                Arg.Is<ClientRequestProperties>(p => p.ClientRequestId != null));
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
            .Create(query.GetType(), sessionId);

        _ = query
            .Received(1)
            .ReadResult(reader);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                databaseName: null,
                $"""
                 .set-or-replace stored_query_result ['{queryId}'] with (previewCount = {pageSize}, expiresAfter = 1h) <|
                 {queryText}
                 | serialize row_number = row_number()
                 """.Replace("\r\n", "\n", StringComparison.Ordinal),
                Arg.Is<ClientRequestProperties>(p => p.ClientRequestId != null));
    }
}