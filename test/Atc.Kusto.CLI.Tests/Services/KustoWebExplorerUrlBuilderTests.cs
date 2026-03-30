namespace Atc.Kusto.CLI.Tests.Services;

public sealed class KustoWebExplorerUrlBuilderTests
{
    [Theory]
    [InlineData("https://help.kusto.windows.net", "https://dataexplorer.azure.com")]
    [InlineData("https://mycluster.kusto.data.microsoft.com", "https://dataexplorer.azure.com")]
    [InlineData("https://mycluster.kusto.fabric.microsoft.com", "https://dataexplorer.azure.com")]
    [InlineData("https://mycluster.kusto.usgovcloudapi.net", "https://dataexplorer.azure.us")]
    [InlineData("https://mycluster.kusto.chinacloudapi.cn", "https://dataexplorer.azure.cn")]
    public void Build_RecognizedCluster_ReturnsUrlWithCorrectBase(
        string clusterUrl,
        string expectedBase)
    {
        // Act
        var url = KustoWebExplorerUrlBuilder.Build(
            new Uri(clusterUrl),
            "Samples",
            "print 1");

        // Assert
        url.Should().NotBeNull();
        url!.AbsoluteUri.Should().StartWith(expectedBase);
    }

    [Fact]
    public void Build_PublicCluster_ContainsClusterHostAndDatabase()
    {
        // Act
        var url = KustoWebExplorerUrlBuilder.Build(
            new Uri("https://help.kusto.windows.net"),
            "Samples",
            "StormEvents | take 5");

        // Assert
        url.Should().NotBeNull();
        url!.AbsoluteUri.Should().Contain("/clusters/help.kusto.windows.net/");
        url.AbsoluteUri.Should().Contain("/databases/Samples?");
    }

    [Fact]
    public void Build_UnrecognizedCluster_ReturnsNull()
    {
        // Act
        var url = KustoWebExplorerUrlBuilder.Build(
            new Uri("https://mycluster.example.com"),
            "MyDb",
            "print 1");

        // Assert
        url.Should().BeNull();
    }

    [Fact]
    public void Build_QueryIsCompressedAndDecompressible()
    {
        // Arrange
        var originalQuery = "StormEvents | take 5";

        // Act
        var url = KustoWebExplorerUrlBuilder.Build(
            new Uri("https://help.kusto.windows.net"),
            "Samples",
            originalQuery);

        // Assert
        url.Should().NotBeNull();

        var queryParam = url!.AbsoluteUri.Split("?query=")[1];
        var base64 = Uri.UnescapeDataString(queryParam);
        var compressed = Convert.FromBase64String(base64);

        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        var decompressed = reader.ReadToEnd();

        decompressed.Should().Be(originalQuery);
    }

    [Fact]
    public void Build_DatabaseWithSpecialChars_IsUrlEncoded()
    {
        // Act
        var url = KustoWebExplorerUrlBuilder.Build(
            new Uri("https://help.kusto.windows.net"),
            "My Database",
            "print 1");

        // Assert
        url.Should().NotBeNull();
        url!.AbsoluteUri.Should().Contain("/databases/My%20Database?");
    }

    [Fact]
    public void Build_VeryLongQuery_ReturnsNullWhenUrlExceedsLimit()
    {
        // Arrange - build a string from GUIDs which resists compression
        var sb = new StringBuilder();
        while (sb.Length < 20000)
        {
            sb.Append(Guid.NewGuid().ToString("N"));
        }

        var longQuery = sb.ToString();

        // Act
        var url = KustoWebExplorerUrlBuilder.Build(
            new Uri("https://help.kusto.windows.net"),
            "Samples",
            longQuery);

        // Assert
        url.Should().BeNull();
    }
}