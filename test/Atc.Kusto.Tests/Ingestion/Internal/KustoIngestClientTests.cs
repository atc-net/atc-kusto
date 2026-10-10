namespace Atc.Kusto.Tests.Ingestion.Internal;

public sealed class KustoIngestClientTests
{
    private static readonly Uri ClusterUri = new("https://example.kusto.windows.net");

    [Theory]
    [InlineData(IngestionMethod.Streaming, KustoIngestionStatus.Succeeded)]
    [InlineData(IngestionMethod.Queued, KustoIngestionStatus.Queued)]
    public void ToIngestionStatus_Maps_From_The_Method_The_Service_Used(
        IngestionMethod method,
        KustoIngestionStatus expected)
        => KustoIngestClient.ToIngestionStatus(method).Should().Be(expected);

    [Theory]
    [InlineData(IngestionMode.Streaming)]
    [InlineData(IngestionMode.ManagedStreaming)]
    [InlineData(IngestionMode.Queued)]
    public void Ctor_Builds_And_Disposes_Without_Upload_Containers(
        IngestionMode mode)
    {
        // Arrange
        var credential = new StaticTokenCredential();

        // Act
        var act = () =>
        {
            using var sut = new KustoIngestClient(ClusterUri, credential, [], mode);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(IngestionMode.Streaming)]
    [InlineData(IngestionMode.ManagedStreaming)]
    [InlineData(IngestionMode.Queued)]
    public void Ctor_Builds_And_Disposes_With_Upload_Containers(
        IngestionMode mode)
    {
        // Arrange
        var credential = new StaticTokenCredential();
        var containers = new List<Uri> { new("https://account.blob.core.windows.net/ingest") };

        // Act
        var act = () =>
        {
            using var sut = new KustoIngestClient(ClusterUri, credential, containers, mode);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(IngestStatus.InProgress, KustoIngestionOperationStatus.InProgress)]
    [InlineData(IngestStatus.Succeeded, KustoIngestionOperationStatus.Succeeded)]
    [InlineData(IngestStatus.PartialSuccess, KustoIngestionOperationStatus.PartialSuccess)]
    [InlineData(IngestStatus.Failed, KustoIngestionOperationStatus.Failed)]
    [InlineData(IngestStatus.Cancelled, KustoIngestionOperationStatus.Cancelled)]
    public void ToOperationStatus_Maps_Each_Value(
        IngestStatus status,
        KustoIngestionOperationStatus expected)
        => KustoIngestClient.ToOperationStatus(status).Should().Be(expected);

    [Theory]
    [InlineData("da-DK")]
    [InlineData("en-DK")]
    [InlineData("en-US")]
    public void OperationHandle_Round_Trips_Under_Any_Culture(string culture)
    {
        // Arrange: a handle contains the operation's startTime. V2's ToJsonString writes it with the
        // current culture's time separator ("20.33.00" under da-DK/en-DK), which FromJsonString can't read.
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

        try
        {
            // Act
            var handle = TestOperationHandles.CreateHandle(IngestionMethod.Queued, "op-42");
            var operation = KustoIngestClient.ParseOperationHandle(handle);

            // Assert
            handle.Should().MatchRegex("\"startTime\":\"[^\"]*T\\d{2}:\\d{2}:\\d{2}");
            operation.Id.Should().Be("op-42");
            operation.IngestionMethod.Should().Be(IngestionMethod.Queued);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("not a handle")]
    [InlineData("{\"startTime\":\"yesterday\"}")]
    public void ParseOperationHandle_Rejects_An_Invalid_Handle(string handle)
    {
        // Act
        var act = () => KustoIngestClient.ParseOperationHandle(handle);

        // Assert
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("operationHandle");
    }

    [Fact]
    public async Task GetOperationStatusAsync_Reports_A_Streamed_Operation_As_Succeeded_Without_Contacting_The_Cluster()
    {
        // Arrange: the cluster URI doesn't exist, so any network call would fail.
        using var sut = new KustoIngestClient(ClusterUri, new StaticTokenCredential(), [], IngestionMode.ManagedStreaming);
        var handle = TestOperationHandles.CreateHandle(IngestionMethod.Streaming, "op-streamed");

        // Act
        var result = await sut.GetOperationStatusAsync(handle, CancellationToken.None);

        // Assert
        result.OperationId.Should().Be("op-streamed");
        result.Status.Should().Be(KustoIngestionOperationStatus.Succeeded);
        result.IsCompleted.Should().BeTrue();
        result.SucceededCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Ctor_Throws_For_Unsupported_Mode()
    {
        // Arrange
        var credential = new StaticTokenCredential();

        // Act
        var act = () => new KustoIngestClient(ClusterUri, credential, [], (IngestionMode)42);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Returns a fixed token without any I/O. The V2 user-containers uploader requests a token
    /// while it is built, so a real credential would make these tests slow and environment-dependent.
    /// </summary>
    private sealed class StaticTokenCredential : Azure.Core.TokenCredential
    {
        public override Azure.Core.AccessToken GetToken(
            Azure.Core.TokenRequestContext requestContext,
            CancellationToken cancellationToken)
            => new("token", DateTimeOffset.MaxValue);

        public override ValueTask<Azure.Core.AccessToken> GetTokenAsync(
            Azure.Core.TokenRequestContext requestContext,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }
}