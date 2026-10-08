namespace Atc.Kusto.Tests.Options;

public sealed class AtcKustoOptionsTests
{
    [Fact]
    public void DefaultIngestionMode_Defaults_To_ManagedStreaming()
        => new AtcKustoOptions().DefaultIngestionMode.Should().Be(IngestionMode.ManagedStreaming);

    [Fact]
    public void IngestUploadContainers_Defaults_To_An_Empty_Mutable_List()
    {
        // Arrange
        var options = new AtcKustoOptions();

        // Act
        options.IngestUploadContainers.Add(new Uri("https://account.blob.core.windows.net/ingest"));

        // Assert
        options.IngestUploadContainers.Should().ContainSingle();
        new AtcKustoOptions().IngestUploadContainers.Should().BeEmpty();
    }
}