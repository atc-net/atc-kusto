namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class BufferedStreamingQueryHandlerTests
{
    private readonly ICslQueryProvider queryProvider;

    private readonly IKustoStreamingQuery<string> query;
    private readonly BufferedStreamingQueryHandler<string> sut;

    public BufferedStreamingQueryHandlerTests()
    {
        queryProvider = Substitute.For<ICslQueryProvider>();
        query = Substitute.For<IKustoStreamingQuery<string>>();

        sut = new BufferedStreamingQueryHandler<string>(
            new NullLogger<BufferedStreamingQueryHandler<string>>(),
            queryProvider,
            query,
            new AtcStreamingQueryOptions { OptionalFrames = FrameHeaders.All, EnableServerSideCancellation = false });
    }

    [Fact]
    public async Task Execute_ShouldIssueCancelCommand_WhenTokenCanceled()
    {
        // Arrange
        var adminProvider = Substitute.For<ICslAdminProvider>();
        var logger = new NullLogger<BufferedStreamingQueryHandler<string>>();
        var options = new AtcStreamingQueryOptions { OptionalFrames = FrameHeaders.All, EnableServerSideCancellation = true };

        query.GetQueryText().Returns("print 1");
        query.MapDataRow(Arg.Any<DataRow>()).Returns((string?)null);

        ClientRequestProperties? capturedProps = null;

        // The cancel command is sent from a background task; signal when it arrives instead of sleeping.
        var cancelCommandSent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelReader = Substitute.For<IDataReader>();

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

        using var pds = ProgressiveDataSetBuilder.BuildPrimaryResult("A");

        var tcs = new TaskCompletionSource<ProgressiveDataSet>();

        queryProvider
            .ExecuteQueryV2Async(
                databaseName: null,
                query.GetQueryText(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedProps = ci.Arg<ClientRequestProperties>();
                var ct = ci.Arg<CancellationToken>();

                // Register cancellation to complete the task
                ct.Register(() => tcs.TrySetCanceled(ct));

                // Don't return the PDS immediately - wait for cancellation
                return tcs.Task;
            });

        var handler = new BufferedStreamingQueryHandler<string>(
            logger,
            adminProvider,
            queryProvider,
            query,
            options);

        using var cts = new CancellationTokenSource();

        // Act - Execute runs synchronously up to the pending query, so the cancellation callback
        // is already registered when it returns; no delay is needed before cancelling.
        var executeTask = handler.Execute(cts.Token);
        await cts.CancelAsync();

        var act = async () => await executeTask;
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

    [Fact]
    public async Task Execute_ShouldNotIssueCancelCommand_WhenServerSideCancelDisabled()
    {
        // Arrange
        var adminProvider = Substitute.For<ICslAdminProvider>();
        var logger = new NullLogger<BufferedStreamingQueryHandler<string>>();
        var options = new AtcStreamingQueryOptions { EnableServerSideCancellation = false };

        query.GetQueryText().Returns("print 1");

        using var pds = ProgressiveDataSetBuilder.BuildPrimaryResult("A");

        queryProvider
            .ExecuteQueryV2Async(
                databaseName: null,
                query.GetQueryText(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ct = ci.Arg<CancellationToken>();
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(ct);
                }

                return pds;
            });

        var handler = new BufferedStreamingQueryHandler<string>(
            logger,
            adminProvider,
            queryProvider,
            query,
            options);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = handler.Execute(cts.Token);
        await cts.CancelAsync();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

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
    public async Task Execute_ShouldReturnMappedRows(
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);

        query.MapDataRow(Arg.Any<DataRow>()).Returns(ci =>
        {
            var row = ci.Args()[0] as DataRow;
            return row?[0]?.ToString();
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
        var result = await sut.Execute(cancellationToken);

        // Assert
        result.Should().NotBeNull();

        var rows = new List<string>();
        await foreach (var r in result!.Rows.WithCancellation(cancellationToken))
        {
            rows.Add(r);
        }

        rows.Should().BeEquivalentTo("A", "B", "C");
        result.Completion.Should().NotBeNull();
        result.Completion!.HasErrors.Should().BeFalse();
    }

    [Theory, AutoNSubstituteData]
    public async Task Execute_ShouldSetError_WhenProviderThrows(
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);

        query.MapDataRow(Arg.Any<DataRow>()).Returns(ci =>
        {
            var row = ci.Args()[0] as DataRow;
            return row?[0]?.ToString();
        });

        // Use a delayed task to simulate the exception being thrown asynchronously
        queryProvider
            .ExecuteQueryV2Async(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProgressiveDataSet>(new InvalidOperationException("boom")));

        // Act
        var result = await sut.Execute(cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Completion.Should().NotBeNull();
        result.Completion!.HasErrors.Should().BeTrue();
        result.Completion.ErrorMessage.Should().Be("boom");
    }

    [Theory]
    [MemberData(nameof(CancellationTestData.SdkCancellationErrors), MemberType = typeof(CancellationTestData))]
    public async Task Execute_Throws_OperationCanceledException_When_The_Sdk_Fails_After_The_Caller_Cancelled(
        Exception sdkError)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        queryProvider
            .ExecuteQueryV2Async(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => CancellationTestData.CancelThenThrow<ProgressiveDataSet>(cts.CancelAsync, sdkError));

        // Act
        var act = () => sut.Execute(cts.Token);

        // Assert
        var thrown = await act.Should().ThrowAsync<OperationCanceledException>();
        thrown.Which.InnerException.Should().BeSameAs(sdkError);
        thrown.Which.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Execute_Reports_A_Kusto_Cancel_Error_As_A_Failure_When_The_Caller_Did_Not_Cancel()
    {
        // Arrange - e.g. someone else ran ".cancel query": a failure, not cancellation by the caller
        queryProvider
            .ExecuteQueryV2Async(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new KustoClientRequestCanceledByUserException());

        // Act
        var result = await sut.Execute(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Completion.Should().NotBeNull();
        result.Completion!.HasErrors.Should().BeTrue();
    }
}

internal static class ProgressiveDataSetBuilder
{
    internal static ProgressiveDataSet BuildPrimaryResult(
        params string[] values)
    {
        var table = new DataTable(WellKnownDataSet.PrimaryResult.ToString());
        table.Columns.Add("col", typeof(string));
        foreach (var v in values)
        {
            table.Rows.Add(v);
        }

        ProgressiveDataSetFrame[] frames =
        {
            new SchemaFrameStub(1, table),
            new DataTableFrameStub(1, table),
            new CompletionFrameStub(),
        };

        var enumerator = ((IEnumerable<ProgressiveDataSetFrame>)frames).GetEnumerator();
        var pds = new ProgressiveDataSet(enumerator);

        return pds;
    }
}

internal sealed class SchemaFrameStub : ProgressiveDataSetDataTableSchemaFrame
{
    public SchemaFrameStub(
        int id,
        DataTable schema)
    {
        FrameType = FrameType.TableHeader;
        TableId = id;
        TableKind = WellKnownDataSet.PrimaryResult;
        TableName = schema.TableName;
        TableSchema = schema;
    }

    public FrameType FrameType { get; }

    public int TableId { get; }

    public string TableName { get; }

    public WellKnownDataSet TableKind { get; }

    public DataTable TableSchema { get; }

    public DataTable ToEmptyDataTable() => TableSchema.Clone();
}

internal sealed class DataTableFrameStub : ProgressiveDataSetDataTableFrame
{
    public DataTableFrameStub(
        int id,
        DataTable sourceTable)
    {
        FrameType = FrameType.DataTable; // Use DataTable frame type to match handler's expectations
        TableId = id;
        TableKind = WellKnownDataSet.PrimaryResult;
        TableName = sourceTable.TableName;
        TableData = new SingleColumnReader(sourceTable);
    }

    public FrameType FrameType { get; }

    public int TableId { get; }

    public string TableName { get; }

    public WellKnownDataSet TableKind { get; }

    public IDataReader TableData { get; }
}

internal sealed class CompletionFrameStub : ProgressiveDataSetCompletionFrame
{
    public CompletionFrameStub(
        bool cancelled = false,
        bool hasErrors = false,
        Exception? ex = null)
    {
        FrameType = FrameType.DataSetCompletion;
        Cancelled = cancelled;
        HasErrors = hasErrors;
        Exception = ex;
    }

    public FrameType FrameType { get; }

    public bool Cancelled { get; }

    public bool HasErrors { get; }

    public Exception? Exception { get; }
}

internal sealed class SingleColumnReader : DbDataReader
{
    ////───────────────────────────────────────────────────────────────────────────────
    ////  Construction
    ////───────────────────────────────────────────────────────────────────────────────
    private readonly IEnumerator<string?> enumerator;

    private readonly DataTable schemaTable;

    public SingleColumnReader(DataTable source)
    {
        var list = new List<string?>();
        foreach (DataRow r in source.Rows)
        {
            list.Add(r[0]?.ToString());
        }

        enumerator = list.GetEnumerator();
        schemaTable = BuildSchemaTable();
    }

    ////───────────────────────────────────────────────────────────────────────────────
    ////  Core iteration
    ////───────────────────────────────────────────────────────────────────────────────
    public override bool Read() => enumerator.MoveNext();

    public override int FieldCount => 1;

    public override bool HasRows => true;

    public override bool NextResult() => false;

    public override void Close()
    {
        /* nothing to dispose */
    }

    ////───────────────────────────────────────────────────────────────────────────────
    ////  Row access
    ////───────────────────────────────────────────────────────────────────────────────
    public override string GetName(int ordinal) => "col";

    public override int GetOrdinal(string name) => 0;

    public override Type GetFieldType(int ordinal) => typeof(string);

    public override object GetValue(int ordinal) => enumerator.Current!;

    public override bool IsDBNull(int ordinal) => enumerator.Current is null;

    public override int GetValues(object[] values)
    {
        if (values is { Length: > 0 })
        {
            values[0] = GetValue(0);
            return 1;
        }

        return 0;
    }

    ////───────────────────────────────────────────────────────────────────────────────
    ////  Strong-typed getters (only string is expected in tests)
    ////───────────────────────────────────────────────────────────────────────────────
    public override string GetString(int ordinal) => (string)GetValue(ordinal);

    ////───────────────────────────────────────────────────────────────────────────────
    ////  Schema
    ////───────────────────────────────────────────────────────────────────────────────
    public override DataTable GetSchemaTable() => schemaTable;

    private static DataTable BuildSchemaTable()
    {
        var t = new DataTable("SchemaTable");

        // ADO-NET columns required by DataTable.Load
        t.Columns.Add("ColumnName", typeof(string));
        t.Columns.Add("ColumnOrdinal", typeof(int));
        t.Columns.Add("DataType", typeof(Type));
        t.Columns.Add("AllowDBNull", typeof(bool));

        // Kusto-specific column used by handler
        t.Columns.Add("ColumnType", typeof(string));

        // one string column
        t.Rows.Add("col", 0, typeof(string), true, "string");
        return t;
    }

    ////───────────────────────────────────────────────────────────────────────────────
    ////  Members we don’t need – return defaults or throw (they’re never hit in tests)
    ////───────────────────────────────────────────────────────────────────────────────
    public override object this[int ordinal] => GetValue(ordinal);

    public override object this[string name] => GetValue(0);

    public override int Depth => 0;

    public override bool IsClosed => false;

    public override int RecordsAffected => -1;

    public override IEnumerator GetEnumerator()
        => throw new NotSupportedException();

    public override string GetDataTypeName(int ordinal) => "string";

    public override long GetBytes(
        int ordinal,
        long dataOffset,
        byte[]? buffer,
        int bufferOffset,
        int length)
        => throw new NotSupportedException();

    public override long GetChars(
        int ordinal,
        long dataOffset,
        char[]? buffer,
        int bufferOffset,
        int length)
        => throw new NotSupportedException();

    public override bool GetBoolean(int ordinal)
        => throw new NotSupportedException();

    public override byte GetByte(int ordinal)
        => throw new NotSupportedException();

    public override char GetChar(int ordinal)
        => throw new NotSupportedException();

    public override DateTime GetDateTime(int ordinal)
        => throw new NotSupportedException();

    public override decimal GetDecimal(int ordinal)
        => throw new NotSupportedException();

    public override double GetDouble(int ordinal)
        => throw new NotSupportedException();

    public override float GetFloat(int ordinal)
        => throw new NotSupportedException();

    public override Guid GetGuid(int ordinal)
        => throw new NotSupportedException();

    public override short GetInt16(int ordinal)
        => throw new NotSupportedException();

    public override int GetInt32(int ordinal)
        => throw new NotSupportedException();

    public override long GetInt64(int ordinal)
        => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            enumerator.Dispose();
            schemaTable.Dispose();
        }

        base.Dispose(disposing);
    }
}