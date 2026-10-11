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
            KustoJsonSerializerOptions.Default,
            new AtcStreamingQueryOptions { OptionalFrames = FrameHeaders.All, EnableServerSideCancellation = false });
    }

    [Theory, AutoNSubstituteData]
    public async Task Execute_ShouldIssueCancelCommand_WhenTokenCanceled(
        ICslAdminProvider adminProvider,
        IDataReader cancelReader)
    {
        // Arrange
        var logger = new NullLogger<StreamingQueryHandler<string>>();
        var options = new AtcStreamingQueryOptions { OptionalFrames = FrameHeaders.All, EnableServerSideCancellation = true };

        query.GetQueryText().Returns("print 1");
        query.MapRecord(Arg.Any<IDataRecord>(), Arg.Any<JsonSerializerOptions>()).Returns((string?)null);

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

        queryProvider
            .ExecuteQueryV2Async(
                databaseName: null,
                query.GetQueryText(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedProps = ci.Arg<ClientRequestProperties>();
                return pds;
            });

        var handler = new StreamingQueryHandler<string>(
            logger,
            adminProvider,
            queryProvider,
            query,
            KustoJsonSerializerOptions.Default,
            options);

        using var cts = new CancellationTokenSource();

        // Act - cancel right after starting enumeration
        var iterator = handler.Execute(cts.Token).GetAsyncEnumerator(cts.Token);
        await cts.CancelAsync();

        try
        {
            _ = await iterator.MoveNextAsync();
        }
        catch
        {
            // ignored
        }
        finally
        {
            await iterator.DisposeAsync();
        }

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
    public async Task Execute_ShouldRethrowProviderException_WhenProviderThrows(
        string queryText,
        CancellationToken cancellationToken)
    {
        // Arrange
        query.GetQueryText().Returns(queryText);

        query.MapRecord(Arg.Any<IDataRecord>(), Arg.Any<JsonSerializerOptions>()).Returns(ci =>
        {
            var row = ci.Args()[0] as IDataRecord;
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

        query.MapRecord(Arg.Any<IDataRecord>(), Arg.Any<JsonSerializerOptions>()).Returns(ci =>
        {
            var row = ci.Args()[0] as IDataRecord;
            return row?[0].ToString();
        });

        // A primary result sent as one DataTable frame
        using var table = new DataTable(nameof(WellKnownDataSet.PrimaryResult));
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

    [Fact]
    public async Task Execute_Maps_Fragment_Rows_With_Each_Value_Converted_To_Its_Column_Type()
    {
        // Arrange - progressive frames send the values as strings
        using var schema = new DataTable(nameof(WellKnownDataSet.PrimaryResult));
        schema.Columns.Add("id", typeof(Guid));
        schema.Columns.Add("amount", typeof(double));

        using var result = ReturnFrames(
        [
            new SchemaFrameStub(1, schema),
            new FragmentFrameStub(
                1,
                ["74be27de-1e4e-49d9-b579-fe0b331d3642", "2.5"],
                ["0b5c4a8e-3f0d-4c1e-9a3b-2d6f7e8a9b10", "-1"]),
            new CompletionFrameStub(),
        ]);

        query
            .MapRecord(Arg.Any<IDataRecord>(), Arg.Any<JsonSerializerOptions>())
            .Returns(ci => string.Create(CultureInfo.InvariantCulture, $"{ci.Arg<IDataRecord>().GetGuid(0)}|{ci.Arg<IDataRecord>().GetDouble(1)}"));

        // Act
        var rows = new List<string?>();
        await foreach (var row in sut.Execute(CancellationToken.None))
        {
            rows.Add(row);
        }

        // Assert
        rows.Should().Equal("74be27de-1e4e-49d9-b579-fe0b331d3642|2.5", "0b5c4a8e-3f0d-4c1e-9a3b-2d6f7e8a9b10|-1");
    }

    [Fact]
    public async Task Execute_Uses_A_DataReplace_That_Comes_Before_Any_Row_Was_Returned()
    {
        // Arrange - nothing was handed out yet, so the replacing rows simply are the result
        using var result = ReturnFrames(ProgressiveDataSetBuilder.BuildProgressiveFrames(
            new FragmentFrameStub(1, TableFragmentType.DataReplace, ["A"], ["B"])));

        MapFirstColumn();

        // Act
        var rows = new List<string?>();
        await foreach (var row in sut.Execute(CancellationToken.None))
        {
            rows.Add(row);
        }

        // Assert
        rows.Should().Equal("A", "B");
    }

    [Fact]
    public async Task Execute_Throws_When_A_DataReplace_Replaces_Rows_That_Were_Already_Returned()
    {
        // Arrange - a preview row is handed out, then the final result replaces it
        using var result = ReturnFrames(ProgressiveDataSetBuilder.BuildProgressiveFrames(
            new FragmentFrameStub(1, TableFragmentType.DataAppend, ["preview"]),
            new FragmentFrameStub(1, TableFragmentType.DataReplace, ["final"])));
        MapFirstColumn();

        var rows = new List<string?>();

        // Act
        var act = async () =>
        {
            await foreach (var row in sut.Execute(CancellationToken.None))
            {
                rows.Add(row);
            }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ProgressiveEnabled*");
        rows.Should().Equal("preview");
    }

    [Fact]
    public async Task Execute_Throws_NotSupported_For_A_DataRemove_Fragment()
    {
        // Arrange
        using var result = ReturnFrames(ProgressiveDataSetBuilder.BuildProgressiveFrames(
            new FragmentFrameStub(1, TableFragmentType.DataRemove, ["A"])));
        MapFirstColumn();

        // Act
        var act = async () =>
        {
            await foreach (var row in sut.Execute(CancellationToken.None))
            {
                _ = row;
            }
        };

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*DataRemove*");
    }

    [Fact]
    public async Task Execute_Reads_Every_Table_To_The_End_And_Closes_It()
    {
        // Arrange - the SDK can't move to the next frame before the current table is read and closed
        var frames = ProgressiveDataSetBuilder.BuildDataTableFrames("A", "B");
        using var result = ReturnFrames([.. frames, new CompletionFrameStub()]);
        MapFirstColumn();

        // Act
        var rows = new List<string?>();
        await foreach (var row in sut.Execute(CancellationToken.None))
        {
            rows.Add(row);
        }

        // Assert
        rows.Should().Equal("A", "B");
        frames.Should().AllSatisfy(frame =>
        {
            frame.Reader.WasReadToEnd.Should().BeTrue();
            frame.Reader.IsClosed.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Execute_Disposes_The_Result_Stream_After_Reading_All_Rows()
    {
        // Arrange
        using var result = ReturnTrackedResult("A", "B");
        MapFirstColumn();

        // Act
        await foreach (var row in sut.Execute(CancellationToken.None))
        {
            _ = row;
        }

        // Assert
        result.AreFramesDisposed.Should().BeTrue();
        result.IsDataSetDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_Disposes_The_Result_Stream_When_The_Caller_Stops_Early()
    {
        // Arrange
        using var result = ReturnTrackedResult("A", "B");
        MapFirstColumn();

        // Act - read the first row, then stop
        await using (var rows = sut.Execute(CancellationToken.None).GetAsyncEnumerator())
        {
            (await rows.MoveNextAsync()).Should().BeTrue();
            rows.Current.Should().Be("A");
        }

        // Assert
        result.AreFramesDisposed.Should().BeTrue();
        result.IsDataSetDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_Disposes_The_Result_Stream_When_Mapping_Fails()
    {
        // Arrange
        using var result = ReturnTrackedResult("A", "B");
        query.MapRecord(Arg.Any<IDataRecord>(), Arg.Any<JsonSerializerOptions>()).Throws(new InvalidOperationException("mapping failed"));

        // Act
        var act = async () =>
        {
            await foreach (var row in sut.Execute(CancellationToken.None))
            {
                _ = row;
            }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        result.AreFramesDisposed.Should().BeTrue();
        result.IsDataSetDisposed.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(CancellationTestData.SdkCancellationErrors), MemberType = typeof(CancellationTestData))]
    public async Task Execute_Throws_OperationCanceledException_When_Starting_The_Stream_Fails_After_The_Caller_Cancelled(
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
        var act = async () =>
        {
            await using var enumerator = sut.Execute(cts.Token).GetAsyncEnumerator(cts.Token);
            await enumerator.MoveNextAsync();
        };

        // Assert
        var thrown = await act.Should().ThrowAsync<OperationCanceledException>();
        thrown.Which.InnerException.Should().BeSameAs(sdkError);
    }

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
            .MapRecord(Arg.Any<IDataRecord>(), Arg.Any<JsonSerializerOptions>())
            .Returns(ci => ci.Arg<IDataRecord>()[0].ToString());

    [Fact]
    public async Task Execute_Passes_Its_SerializerOptions_To_MapRecord()
    {
        // Arrange
        var serializerOptions = new JsonSerializerOptions();
        using var result = ReturnFrames(ProgressiveDataSetBuilder.BuildProgressiveFrames(
            new FragmentFrameStub(1, ["A"])));

        var handler = new StreamingQueryHandler<string>(
            new NullLogger<StreamingQueryHandler<string>>(),
            queryProvider,
            query,
            serializerOptions,
            new AtcStreamingQueryOptions { OptionalFrames = FrameHeaders.All, EnableServerSideCancellation = false });

        // Act
        await foreach (var row in handler.Execute(CancellationToken.None))
        {
            _ = row;
        }

        // Assert
        query
            .Received(1)
            .MapRecord(Arg.Any<IDataRecord>(), serializerOptions);
    }
}