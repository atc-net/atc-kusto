namespace Atc.Kusto.Tests.Ingestion.Internal;

public sealed class KustoIngestorTests
{
    private readonly IKustoIngestClientProvider provider = Substitute.For<IKustoIngestClientProvider>();
    private readonly IKustoIngestClient client = Substitute.For<IKustoIngestClient>();
    private readonly AtcKustoOptions options = new()
    {
        HostAddress = new Uri("https://example.kusto.windows.net"),
        DatabaseName = "DefaultDb",
        Credential = Substitute.For<Azure.Core.TokenCredential>(),
    };

    private readonly KustoIngestor sut;

    public KustoIngestorTests()
    {
        var monitor = Substitute.For<IOptionsMonitor<AtcKustoOptions>>();
        monitor.Get(Arg.Any<string?>()).Returns(options);

        provider
            .GetIngestClient(Arg.Any<IngestionMode>(), Arg.Any<string?>())
            .Returns(client);

        ReturnsFromClient(KustoIngestionStatus.Succeeded);

        sut = new KustoIngestor(NullLogger<KustoIngestor>.Instance, provider, monitor);
    }

    [Fact]
    public async Task IngestAsync_Rows_Returns_The_Client_Result_And_Passes_The_Request_Through()
    {
        // Act
        var result = await sut.IngestAsync([new Row("a")], JsonTarget() with { DatabaseName = "OtherDb" });

        // Assert
        result.Status.Should().Be(KustoIngestionStatus.Succeeded);
        await client.Received(1).IngestStreamAsync(
            Arg.Any<Stream>(),
            KustoIngestFormat.MultiJson,
            "OtherDb",
            "Events",
            "Events_mapping",
            false,
            IngestionMode.ManagedStreaming,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_Rows_Writes_CamelCase_MultiJson_By_Default()
    {
        // Arrange
        string? payload = null;
        client
            .IngestStreamAsync(
                data: null,
                format: default,
                database: null,
                table: null,
                mappingReference: null,
                enableTracking: false,
                mode: default,
                cancellationToken: CancellationToken.None)
            .ReturnsForAnyArgs(call =>
            {
                payload = Encoding.UTF8.GetString(((MemoryStream)call.Arg<Stream>()).ToArray());
                return Result(KustoIngestionStatus.Succeeded);
            });

        // Act
        await sut.IngestAsync([new Row("a"), new Row("b")], JsonTarget());

        // Assert
        payload.Should().Be("{\"name\":\"a\"}\n{\"name\":\"b\"}\n");
    }

    [Fact]
    public async Task IngestAsync_Uses_The_Connection_Default_Mode_When_Target_Mode_Is_Null()
    {
        // Arrange
        options.DefaultIngestionMode = IngestionMode.Queued;

        // Act
        await sut.IngestAsync([new Row("a")], JsonTarget());

        // Assert
        provider.Received(1).GetIngestClient(IngestionMode.Queued);
    }

    [Fact]
    public async Task IngestAsync_Target_Mode_Overrides_The_Connection_Default()
    {
        // Arrange
        options.DefaultIngestionMode = IngestionMode.Queued;

        // Act
        await sut.IngestAsync([new Row("a")], JsonTarget() with { Mode = IngestionMode.Streaming });

        // Assert
        provider.Received(1).GetIngestClient(IngestionMode.Streaming);
    }

    [Fact]
    public async Task IngestAsync_Returns_Queued_Result_From_The_Client()
    {
        // Arrange
        ReturnsFromClient(KustoIngestionStatus.Queued);

        // Act
        var result = await sut.IngestAsync([new Row("a")], JsonTarget());

        // Assert
        result.Status.Should().Be(KustoIngestionStatus.Queued);
    }

    [Fact]
    public async Task IngestAsync_Returns_Failed_With_Message_When_The_Client_Throws()
    {
        // Arrange
        client
            .IngestStreamAsync(
                data: null,
                format: default,
                database: null,
                table: null,
                mappingReference: null,
                enableTracking: false,
                mode: default,
                cancellationToken: CancellationToken.None)
            .ThrowsAsyncForAnyArgs(new InvalidOperationException("boom"));

        // Act
        var result = await sut.IngestAsync([new Row("a")], JsonTarget());

        // Assert
        result.Status.Should().Be(KustoIngestionStatus.Failed);
        result.Mode.Should().Be(IngestionMode.ManagedStreaming);
        result.ErrorMessage.Should().Contain("boom");
    }

    [Fact]
    public async Task IngestAsync_Returns_Failed_When_Creating_The_Client_Fails()
    {
        // Arrange: e.g. the credential cannot get a token while the upload-containers uploader is built.
        provider
            .GetIngestClient(Arg.Any<IngestionMode>(), Arg.Any<string?>())
            .Throws(new InvalidOperationException("token unavailable"));

        // Act
        var result = await sut.IngestAsync([new Row("a")], JsonTarget());

        // Assert
        result.Status.Should().Be(KustoIngestionStatus.Failed);
        result.ErrorMessage.Should().Contain("token unavailable");
    }

    [Fact]
    public async Task IngestAsync_Throws_When_The_Connection_Has_No_Credential()
    {
        // Arrange
        options.Credential = null;

        // Act
        var act = () => sut.IngestAsync([new Row("a")], JsonTarget());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*HostAddress and Credential*");
        provider.DidNotReceiveWithAnyArgs().GetIngestClient(default);
    }

    [Fact]
    public Task IngestAsync_Rows_Rejects_A_Non_Json_Format_Before_Enumerating_The_Rows()
    {
        // Act
        var act = () => sut.IngestAsync(RowsThatMustNotBeEnumerated(), JsonTarget() with { Format = KustoIngestFormat.Csv });

        // Assert
        return act.Should().ThrowAsync<ArgumentException>().WithMessage("*Json or MultiJson*");
    }

    [Fact]
    public Task IngestAsync_Throws_When_No_Database_Can_Be_Resolved_Before_Enumerating_The_Rows()
    {
        // Arrange
        options.DatabaseName = null;

        // Act
        var act = () => sut.IngestAsync(RowsThatMustNotBeEnumerated(), JsonTarget());

        // Assert
        return act.Should().ThrowAsync<ArgumentException>().WithMessage("*No database*");
    }

    [Fact]
    public async Task IngestAsync_Rows_Returns_Skipped_For_No_Rows_Without_Calling_The_Cluster()
    {
        // Act
        var result = await sut.IngestAsync(Array.Empty<Row>(), JsonTarget());

        // Assert
        result.Status.Should().Be(KustoIngestionStatus.Skipped);
        provider.DidNotReceiveWithAnyArgs().GetIngestClient(default);
    }

    [Fact]
    public async Task IngestAsync_Stream_Returns_Skipped_When_Nothing_Remains_After_The_Current_Position()
    {
        // Arrange
        using var data = new MemoryStream([1, 2, 3]);
        data.Position = data.Length;

        // Act
        var result = await sut.IngestAsync(data, CsvTarget());

        // Assert
        result.Status.Should().Be(KustoIngestionStatus.Skipped);
        provider.DidNotReceiveWithAnyArgs().GetIngestClient(default);
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Stream_Rejects_A_Non_Seekable_Stream(
        Stream data)
    {
        // Arrange
        data.CanSeek.Returns(false);

        // Act
        var act = () => sut.IngestAsync(data, CsvTarget());

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("data");
    }

    [Theory, AutoNSubstituteData]
    public Task IngestAsync_Stream_Rejects_More_Than_10_MB_For_Streaming(
        Stream data)
    {
        // Arrange
        data.CanSeek.Returns(true);
        data.Length.Returns(KustoIngestTargetValidator.StreamingIngestionMaxBytes + 1);

        // Act
        var act = () => sut.IngestAsync(data, CsvTarget() with { Mode = IngestionMode.Streaming });

        // Assert
        return act.Should().ThrowAsync<ArgumentException>().WithMessage("*10 MB*");
    }

    [Fact]
    public async Task IngestAsync_Throws_OperationCanceled_For_An_Already_Cancelled_Token()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => sut.IngestAsync([new Row("a")], JsonTarget(), cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        provider.DidNotReceiveWithAnyArgs().GetIngestClient(default);
    }

    [Fact]
    public async Task IngestAsync_Translates_Any_Exception_To_OperationCanceled_When_The_Caller_Cancelled()
    {
        // Arrange: the SDK surfaces the cancelled request as a transport error, not an OperationCanceledException.
        using var cts = new CancellationTokenSource();
        var transportError = new HttpRequestException("connection closed");
        client
            .IngestStreamAsync(
                data: null,
                format: default,
                database: null,
                table: null,
                mappingReference: null,
                enableTracking: false,
                mode: default,
                cancellationToken: CancellationToken.None)
            .ReturnsForAnyArgs<Task<KustoIngestionResult>>(async _ =>
            {
                await cts.CancelAsync();
                throw transportError;
            });

        // Act
        var act = () => sut.IngestAsync([new Row("a")], JsonTarget(), cancellationToken: cts.Token);

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.InnerException.Should().BeSameAs(transportError);
    }

    [Fact]
    public async Task IngestAsync_Reports_An_Sdk_Timeout_As_Failed_When_The_Caller_Did_Not_Cancel()
    {
        // Arrange
        client
            .IngestStreamAsync(
                data: null,
                format: default,
                database: null,
                table: null,
                mappingReference: null,
                enableTracking: false,
                mode: default,
                cancellationToken: default)
            .ThrowsAsyncForAnyArgs(new TaskCanceledException("timed out"));

        // Act
        var result = await sut.IngestAsync([new Row("a")], JsonTarget());

        // Assert
        result.Status.Should().Be(KustoIngestionStatus.Failed);
    }

    [Fact]
    public async Task IngestFromBlobAsync_Passes_The_Blob_Through()
    {
        // Arrange
        var blobUri = new Uri("https://account.blob.core.windows.net/c/data.csv?sv=sas");
        client
            .IngestBlobAsync(
                blobUri: null,
                format: default,
                database: null,
                table: null,
                mappingReference: null,
                enableTracking: false,
                mode: default,
                cancellationToken: default)
            .ReturnsForAnyArgs(Result(KustoIngestionStatus.Queued));

        // Act
        var result = await sut.IngestFromBlobAsync(blobUri, CsvTarget());

        // Assert
        result.Status.Should().Be(KustoIngestionStatus.Queued);
        await client.Received(1).IngestBlobAsync(
            blobUri,
            KustoIngestFormat.Csv,
            "DefaultDb",
            "Events",
            null,
            false,
            IngestionMode.ManagedStreaming,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestFromBlobAsync_Rejects_A_Relative_Uri()
    {
        // Act
        var act = () => sut.IngestFromBlobAsync(new Uri("c/data.csv", UriKind.Relative), CsvTarget());

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("blobUri");
    }

    [Fact]
    public async Task GetIngestionStatusAsync_Asks_The_Queued_Client_Of_The_Given_Connection()
    {
        // Arrange
        var handle = TestOperationHandles.CreateHandle(IngestionMethod.Queued);
        var expected = new KustoIngestionOperationResult { OperationId = "op-123", Status = KustoIngestionOperationStatus.InProgress };
        client
            .GetOperationStatusAsync(
                operationHandle: null,
                cancellationToken: CancellationToken.None)
            .ReturnsForAnyArgs(expected);

        // Act
        var result = await sut.GetIngestionStatusAsync(handle, "Sales");

        // Assert
        result.Should().BeSameAs(expected);
        provider.Received(1).GetIngestClient(IngestionMode.Queued, "Sales");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a handle")]
    public async Task GetIngestionStatusAsync_Rejects_An_Invalid_Handle_Without_Contacting_The_Cluster(
        string handle)
    {
        // Act
        var act = () => sut.GetIngestionStatusAsync(handle);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        provider.DidNotReceiveWithAnyArgs().GetIngestClient(default);
    }

    [Fact]
    public Task GetIngestionStatusAsync_Throws_When_The_Connection_Has_No_Credential()
    {
        // Arrange
        options.Credential = null;

        // Act
        var act = () => sut.GetIngestionStatusAsync(TestOperationHandles.CreateHandle(IngestionMethod.Queued));

        // Assert
        return act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*HostAddress and Credential*");
    }

    [Fact]
    public async Task GetIngestionStatusAsync_Throws_KustoIngestionException_When_The_Check_Fails()
    {
        // Arrange
        var networkError = new HttpRequestException("cluster unreachable");

        client
            .GetOperationStatusAsync(
                operationHandle: null,
                cancellationToken: CancellationToken.None)
            .ThrowsAsyncForAnyArgs(networkError);

        // Act
        var act = () => sut.GetIngestionStatusAsync(TestOperationHandles.CreateHandle(IngestionMethod.Queued));

        // Assert
        var exception = (await act.Should().ThrowAsync<KustoIngestionException>()).Which;
        exception.InnerException.Should().BeSameAs(networkError);
        exception.Message.Should().Contain("cluster unreachable");
    }

    [Fact]
    public async Task GetIngestionStatusAsync_Throws_OperationCanceled_When_The_Caller_Cancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => sut.GetIngestionStatusAsync(TestOperationHandles.CreateHandle(IngestionMethod.Queued), cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        provider.DidNotReceiveWithAnyArgs().GetIngestClient(default);
    }

    private static KustoIngestTarget JsonTarget()
        => new()
        {
            TableName = "Events",
            Format = KustoIngestFormat.MultiJson,
            MappingReference = "Events_mapping",
        };

    private static KustoIngestTarget CsvTarget()
        => new()
        {
            TableName = "Events",
            Format = KustoIngestFormat.Csv,
        };

    private static IEnumerable<Row> RowsThatMustNotBeEnumerated()
    {
        throw new InvalidOperationException("The rows were enumerated before validation finished.");
#pragma warning disable CS0162 // Unreachable code: the yield makes this an iterator, so the throw runs on enumeration.
        yield break;
#pragma warning restore CS0162
    }

    private static Task<KustoIngestionResult> Result(
        KustoIngestionStatus status)
        => Task.FromResult(new KustoIngestionResult { Status = status, Mode = IngestionMode.ManagedStreaming });

    private void ReturnsFromClient(KustoIngestionStatus status)
        => client
            .IngestStreamAsync(
                data: null,
                format: default,
                database: null,
                table: null,
                mappingReference: null,
                enableTracking: false,
                mode: default,
                cancellationToken: CancellationToken.None)
            .ReturnsForAnyArgs(Result(status));

    private sealed record Row(string Name);
}