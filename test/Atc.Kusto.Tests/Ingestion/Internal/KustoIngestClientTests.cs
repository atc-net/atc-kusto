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