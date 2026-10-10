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

    [Theory, AutoNSubstituteData]
    public async Task Execute_ShouldIssueCancelCommand_WhenTokenCanceled(
        ICslAdminProvider adminProvider,
        IDataReader cancelReader)
    {
        // Arrange
        var logger = new NullLogger<BufferedStreamingQueryHandler<string>>();
        var options = new AtcStreamingQueryOptions { OptionalFrames = FrameHeaders.All, EnableServerSideCancellation = true };

        query.GetQueryText().Returns("print 1");
        query.MapRecord(Arg.Any<IDataRecord>()).Returns((string?)null);

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

    [Theory, AutoNSubstituteData]
    public async Task Execute_ShouldNotIssueCancelCommand_WhenServerSideCancelDisabled(
        ICslAdminProvider adminProvider)
    {
        // Arrange
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

        query.MapRecord(Arg.Any<IDataRecord>()).Returns(ci =>
        {
            var row = ci.Args()[0] as IDataRecord;
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

        query.MapRecord(Arg.Any<IDataRecord>()).Returns(ci =>
        {
            var row = ci.Args()[0] as IDataRecord;
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
    public async Task Execute_Maps_Fragment_Rows_With_Each_Value_Converted_To_Its_Column_Type()
    {
        // Arrange - progressive frames send the values as strings
        using var result = ReturnFrames(BuildFragmentFrames());
        query.MapRecord(Arg.Any<IDataRecord>()).Returns(ci => DescribeIdAndAmount(ci.Arg<IDataRecord>()));

        // Act
        var queryResult = await sut.Execute(CancellationToken.None);

        // Assert
        var rows = new List<string?>();
        await foreach (var row in queryResult!.Rows)
        {
            rows.Add(row);
        }

        rows.Should().Equal("74be27de-1e4e-49d9-b579-fe0b331d3642|2.5", "0b5c4a8e-3f0d-4c1e-9a3b-2d6f7e8a9b10|-1");
    }

    [Fact]
    public async Task Execute_Applies_A_DataReplace_Before_Handing_Out_Rows()
    {
        // Arrange - a preview of two rows, then the final result replacing it
        using var result = ReturnFrames(ProgressiveDataSetBuilder.BuildProgressiveFrames(
            new FragmentFrameStub(1, TableFragmentType.DataAppend, ["preview-1"], ["preview-2"]),
            new FragmentFrameStub(1, TableFragmentType.DataReplace, ["final"])));

        MapFirstColumn();

        // Act
        var queryResult = await sut.Execute(CancellationToken.None);

        // Assert
        var rows = new List<string?>();
        await foreach (var row in queryResult!.Rows)
        {
            rows.Add(row);
        }

        rows.Should().Equal("final");
        queryResult.Completion!.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task Execute_Reports_A_DataRemove_Fragment_As_An_Error()
    {
        // Arrange
        using var result = ReturnFrames(ProgressiveDataSetBuilder.BuildProgressiveFrames(
            new FragmentFrameStub(1, TableFragmentType.DataAppend, ["A"]),
            new FragmentFrameStub(1, TableFragmentType.DataRemove, ["A"])));
        MapFirstColumn();

        // Act
        var queryResult = await sut.Execute(CancellationToken.None);

        // Assert
        queryResult!.Completion!.HasErrors.Should().BeTrue();
        queryResult.Completion.ErrorMessage.Should().Contain("DataRemove").And.Contain("ProgressiveEnabled");
    }

    [Fact]
    public async Task Execute_Reads_Every_Table_To_The_End_And_Closes_It()
    {
        // Arrange - the SDK can't move to the next frame before the current table is read and closed
        var frames = ProgressiveDataSetBuilder.BuildDataTableFrames("A", "B");
        using var result = ReturnFrames([.. frames, new CompletionFrameStub()]);
        MapFirstColumn();

        // Act
        var queryResult = await sut.Execute(CancellationToken.None);

        // Assert
        var rows = new List<string?>();
        await foreach (var row in queryResult!.Rows)
        {
            rows.Add(row);
        }

        rows.Should().Equal("A", "B");
        frames.Should().AllSatisfy(frame =>
        {
            frame.Reader.WasReadToEnd.Should().BeTrue();
            frame.Reader.IsClosed.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Execute_Disposes_The_Result_Stream_After_Reading_All_Frames()
    {
        // Arrange
        using var result = ReturnTrackedResult("A", "B");
        MapFirstColumn();

        // Act
        await sut.Execute(CancellationToken.None);

        // Assert
        result.AreFramesDisposed.Should().BeTrue();
        result.IsDataSetDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_Disposes_The_Result_Stream_When_Processing_Fails()
    {
        // Arrange
        using var result = ReturnTrackedResult("A", "B");
        query.MapRecord(Arg.Any<IDataRecord>()).Throws(new InvalidOperationException("mapping failed"));

        // Act
        var queryResult = await sut.Execute(CancellationToken.None);

        // Assert
        queryResult!.Completion!.HasErrors.Should().BeTrue();
        result.AreFramesDisposed.Should().BeTrue();
        result.IsDataSetDisposed.Should().BeTrue();
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

    private static ProgressiveDataSetFrame[] BuildFragmentFrames()
    {
        using var schema = new DataTable(nameof(WellKnownDataSet.PrimaryResult));
        schema.Columns.Add("id", typeof(Guid));
        schema.Columns.Add("amount", typeof(double));

        return
        [
            new SchemaFrameStub(1, schema),
            new FragmentFrameStub(
                1,
                ["74be27de-1e4e-49d9-b579-fe0b331d3642", "2.5"],
                ["0b5c4a8e-3f0d-4c1e-9a3b-2d6f7e8a9b10", "-1"]),
            new CompletionFrameStub(),
        ];
    }

    private static string DescribeIdAndAmount(IDataRecord record)
        => string.Create(CultureInfo.InvariantCulture, $"{record.GetGuid(0)}|{record.GetDouble(1)}");

    private TrackedProgressiveDataSet ReturnFrames(
        IEnumerable<ProgressiveDataSetFrame> frames)
    {
        var result = new TrackedProgressiveDataSet(frames);

        queryProvider
            .ExecuteQueryV2Async(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(result.DataSet);

        return result;
    }

    private TrackedProgressiveDataSet ReturnTrackedResult(
        params string[] values)
    {
        var result = new TrackedProgressiveDataSet(values);

        queryProvider
            .ExecuteQueryV2Async(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(result.DataSet);

        return result;
    }

    private void MapFirstColumn()
        => query
            .MapRecord(Arg.Any<IDataRecord>())
            .Returns(ci => ci.Arg<IDataRecord>()[0].ToString());
}

internal static class ProgressiveDataSetBuilder
{
    internal static ProgressiveDataSet BuildPrimaryResult(
        params string[] values)
    {
        var enumerator = BuildPrimaryResultFrames(values).GetEnumerator();
        var pds = new ProgressiveDataSet(enumerator);

        return pds;
    }

    internal static IEnumerable<ProgressiveDataSetFrame> BuildPrimaryResultFrames(
        params string[] values)
    {
        using var table = BuildTable(WellKnownDataSet.PrimaryResult.ToString(), values);

        return
        [
            new SchemaFrameStub(1, table),
            new DataTableFrameStub(1, table),
            new CompletionFrameStub(),
        ];
    }

    // Frames as Kusto sends them without progressive results: every table in one DataTable frame,
    // with the primary result between the query properties and the completion information.
    internal static DataTableFrameStub[] BuildDataTableFrames(
        params string[] values)
        =>
        [
            BuildDataTableFrame(0, WellKnownDataSet.QueryProperties, "properties"),
            BuildDataTableFrame(1, WellKnownDataSet.PrimaryResult, values),
            BuildDataTableFrame(2, WellKnownDataSet.QueryCompletionInformation, "completion"),
        ];

    // A primary result in progressive mode: the table header, then the given fragments.
    internal static ProgressiveDataSetFrame[] BuildProgressiveFrames(
        params FragmentFrameStub[] fragments)
    {
        using var table = BuildTable(nameof(WellKnownDataSet.PrimaryResult));

        return [new SchemaFrameStub(1, table), .. fragments, new CompletionFrameStub()];
    }

    internal static DataTable BuildTable(
        string name,
        params string[] values)
    {
        var table = new DataTable(name);
        table.Columns.Add("col", typeof(string));
        foreach (var v in values)
        {
            table.Rows.Add(v);
        }

        return table;
    }

    private static DataTableFrameStub BuildDataTableFrame(
        int id,
        WellKnownDataSet tableKind,
        params string[] values)
    {
        // The frame's reader copies the values, so the table can be disposed.
        using var table = BuildTable(tableKind.ToString(), values);
        return new DataTableFrameStub(id, table, tableKind);
    }
}

internal sealed class SchemaFrameStub : ProgressiveDataSetDataTableSchemaFrame
{
    private static readonly Dictionary<Type, string> KustoTypeNames = new()
    {
        [typeof(bool)] = "bool",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(SqlDecimal)] = "decimal",
        [typeof(double)] = "real",
        [typeof(Guid)] = "guid",
        [typeof(object)] = "dynamic",
        [typeof(DateTime)] = "datetime",
        [typeof(TimeSpan)] = "timespan",
        [typeof(string)] = "string",
    };

    /// <summary>
    /// Creates a schema frame for a table with the columns of <paramref name="table"/>.
    /// </summary>
    /// <param name="id">The table id.</param>
    /// <param name="table">A table with the wanted column names and types.</param>
    public SchemaFrameStub(
        int id,
        DataTable table)
    {
        FrameType = FrameType.TableHeader;
        TableId = id;
        TableKind = WellKnownDataSet.PrimaryResult;
        TableName = table.TableName;
        TableSchema = BuildSchemaTable(table);
    }

    public FrameType FrameType { get; }

    public int TableId { get; }

    public string TableName { get; }

    public WellKnownDataSet TableKind { get; }

    public DataTable TableSchema { get; }

    // Like the SDK: one row per column, with the Kusto column type that ToEmptyDataTable() reads.
    private static DataTable BuildSchemaTable(DataTable table)
    {
        var schema = new DataTable("SchemaTable");
        schema.Columns.Add("ColumnName", typeof(string));
        schema.Columns.Add("ColumnOrdinal", typeof(int));
        schema.Columns.Add("DataType", typeof(Type));
        schema.Columns.Add("ColumnType", typeof(string));

        foreach (DataColumn column in table.Columns)
        {
            schema.Rows.Add(
                column.ColumnName,
                column.Ordinal,
                typeof(string),
                KustoTypeNames[column.DataType]);
        }

        return schema;
    }
}

internal sealed class DataTableFrameStub : ProgressiveDataSetDataTableFrame
{
    public DataTableFrameStub(
        int id,
        DataTable sourceTable,
        WellKnownDataSet tableKind = WellKnownDataSet.PrimaryResult)
    {
        FrameType = FrameType.DataTable; // Use DataTable frame type to match handler's expectations
        TableId = id;
        TableKind = tableKind;
        TableName = sourceTable.TableName;
        Reader = new SingleColumnReader(sourceTable);
    }

    public FrameType FrameType { get; }

    public int TableId { get; }

    public string TableName { get; }

    public WellKnownDataSet TableKind { get; }

    public IDataReader TableData => Reader;

    public SingleColumnReader Reader { get; }
}

internal sealed class FragmentFrameStub : ProgressiveDataSetDataTableFragmentFrame
{
    private readonly Queue<object?[]> rows;

    public FragmentFrameStub(
        int id,
        params object?[][] rows)
        : this(id, TableFragmentType.DataAppend, rows)
    {
    }

    public FragmentFrameStub(
        int id,
        TableFragmentType frameSubType,
        params object?[][] rows)
    {
        FrameType = FrameType.TableFragment;
        TableId = id;
        FrameSubType = frameSubType;
        FieldCount = rows[0].Length;
        this.rows = new Queue<object?[]>(rows);
    }

    public FrameType FrameType { get; }

    public int TableId { get; }

    public int FieldCount { get; }

    public TableFragmentType FrameSubType { get; }

    public bool GetNextRecord(object[] values)
    {
        if (!rows.TryDequeue(out var row))
        {
            return false;
        }

        Array.Copy(row, values, row.Length);
        return true;
    }
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

    private bool isClosed;

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
    public override bool Read()
    {
        if (enumerator.MoveNext())
        {
            return true;
        }

        WasReadToEnd = true;
        return false;
    }

    public bool WasReadToEnd { get; private set; }

    public override int FieldCount => 1;

    public override bool HasRows => true;

    public override bool NextResult() => false;

    public override void Close() => isClosed = true;

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

    public override bool IsClosed => isClosed;

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