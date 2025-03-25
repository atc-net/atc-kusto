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
            query);
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