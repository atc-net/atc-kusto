namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class SimpleQueryHandlerTests
{
    [Theory, AutoNSubstituteData]
    internal async Task Execute_ShouldReturnResult_WhenQueryExecutesSuccessfully(
        [Frozen] ICslQueryProvider queryProvider,
        [Frozen] IKustoQuery<string> query,
        [Frozen] IDataReader reader,
        SimpleQueryHandler<string> sut,
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
                databaseName: default,
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
                databaseName: default,
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
        [Frozen] ICslQueryProvider queryProvider,
        [Frozen] IKustoQuery<string?> query,
        [Frozen] IDataReader reader,
        SimpleQueryHandler<string?> sut,
        CancellationToken cancellationToken)
    {
        // Arrange
        queryProvider
            .ExecuteQueryAsync(
                databaseName: default,
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
                databaseName: default,
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