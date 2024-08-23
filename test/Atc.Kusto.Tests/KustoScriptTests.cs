namespace Atc.Kusto.Tests;

public sealed record KustoScriptTests : KustoScript
{
    public string SampleProperty { get; set; } = string.Empty;

    [Fact]
    public void GetQueryText_ShouldReturnQueryText_WhenResourceExists()
    {
        // Arrange
        var resourceName = $"{GetType().FullName}.kusto";
        using var stream = GetType().Assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var expectedResult = reader.ReadToEnd();

        // Act
        var queryText = GetQueryText();

        // Assert
        Assert.Equal(expectedResult, queryText);
    }

    [Fact]
    public void GetParameters_ShouldReturnCorrectParameters_WhenPropertiesArePresent()
    {
        // Arrange
        SampleProperty = "SampleValue";

        // Act
        var parameters = GetParameters();

        // Assert
        Assert.Single(parameters);
        Assert.Equal("sampleProperty", parameters.Keys.First());
        Assert.Equal("SampleValue", parameters.Values.First());
    }
}