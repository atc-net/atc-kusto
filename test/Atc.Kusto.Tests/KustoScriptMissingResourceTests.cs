namespace Atc.Kusto.Tests;

public sealed record KustoScriptMissingResourceTests : KustoScript
{
    [Fact]
    public void GetQueryText_ShouldThrowFileNotFoundException_WhenResourceDoesNotExist()
    {
        // Arrange
        var script = new KustoScriptMissingResourceTests();

        // Act & Assert
        Assert.Throws<FileNotFoundException>(script.GetQueryText);
    }
}