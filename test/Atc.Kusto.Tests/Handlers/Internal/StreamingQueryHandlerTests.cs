namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class StreamingQueryHandlerTests
{
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoStreamingQuery<string> query;
    private readonly StreamingQueryHandler<string> sut;

    public StreamingQueryHandlerTests()
    {
        queryProvider = Substitute.For<ICslQueryProvider>();
        query = Substitute.For<IKustoStreamingQuery<string>>();

        sut = new StreamingQueryHandler<string>(
            new NullLogger<StreamingQueryHandler<string>>(),
            queryProvider,
            query,
            new AtcStreamingQueryOptions { OptionalFrames = FrameHeaders.All });
    }

    [Theory, AutoNSubstituteData]
    public async Task Execute_ShouldYieldMappedRows(
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);

        query.MapDataRow(Arg.Any<DataRow>()).Returns(ci =>
        {
            var row = ci.Args()[0] as DataRow;
            return row?[0].ToString();
        });

        using var pds = ProgressiveDataSetBuilder.BuildPrimaryResult("A", "B", "C");

        queryProvider
            .ExecuteQueryV2Async(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties(),
                cancellationToken)
            .ReturnsForAnyArgs(pds);

        // Act
        var rows = new List<string?>();
        await foreach (var row in sut.Execute(cancellationToken))
        {
            rows.Add(row);
        }

        // Assert
        rows.Should().BeEquivalentTo("A", "B", "C");
    }

    [Theory, AutoNSubstituteData]
    public async Task Execute_ShouldYieldEmptyCollection_WhenProviderThrows(
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);

        query.MapDataRow(Arg.Any<DataRow>()).Returns(ci =>
        {
            var row = ci.Args()[0] as DataRow;
            return row?[0].ToString();
        });

        // Set up the provider to throw an exception
        queryProvider
            .ExecuteQueryV2Async(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProgressiveDataSet>(new InvalidOperationException("boom")));

        // Act
        var rows = new List<string?>();

        try
        {
            await foreach (var row in sut.Execute(cancellationToken))
            {
                rows.Add(row);
            }

            // If no exception was thrown, fail the test
            Assert.Fail("Expected an InvalidOperationException but no exception was thrown");
        }
        catch (InvalidOperationException ex)
        {
            // Assert
            ex.Message.Should().Be("boom");
            rows.Should().BeEmpty();
        }
    }

    [Theory, AutoNSubstituteData]
    public async Task Execute_ShouldYieldMappedRows_FromDataTableFrames(
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);

        query.MapDataRow(Arg.Any<DataRow>()).Returns(ci =>
        {
            var row = ci.Args()[0] as DataRow;
            return row?[0].ToString();
        });

        // Create test frames with DataTable frames instead of TableFragment frames
        var table = new DataTable(nameof(WellKnownDataSet.PrimaryResult));
        table.Columns.Add("col", typeof(string));
        table.Rows.Add("D");
        table.Rows.Add("E");
        table.Rows.Add("F");

        ProgressiveDataSetFrame[] frames =
        [
            new SchemaFrameStub(1, table),
            new DataTableFrameStub(1, table),
            new CompletionFrameStub(),
        ];

        var enumerator = ((IEnumerable<ProgressiveDataSetFrame>)frames).GetEnumerator();
        using var pds = new ProgressiveDataSet(enumerator);

        queryProvider
            .ExecuteQueryV2Async(
                databaseName: null,
                query.GetQueryText(),
                query.GetClientRequestProperties(),
                cancellationToken)
            .ReturnsForAnyArgs(pds);

        // Act
        var rows = new List<string?>();
        await foreach (var row in sut.Execute(cancellationToken))
        {
            rows.Add(row);
        }

        // Assert
        rows.Should().BeEquivalentTo("D", "E", "F");
    }
}