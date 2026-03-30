namespace Atc.Kusto.CLI.Tests.Rendering;

public sealed class OutputFormatParserTests
{
    [Theory]
    [InlineData("human", OutputFormat.Human)]
    [InlineData("json", OutputFormat.Json)]
    [InlineData("markdown", OutputFormat.Markdown)]
    [InlineData("md", OutputFormat.Markdown)]
    [InlineData("csv", OutputFormat.Csv)]
    public void Parse_ValidFormat_ReturnsExpected(
        string input,
        OutputFormat expected)
    {
        // Act
        var result = OutputFormatParser.Parse(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("xml")]
    [InlineData("")]
    public void Parse_UnknownFormat_DefaultsToHuman(string input)
    {
        // Act
        var result = OutputFormatParser.Parse(input);

        // Assert
        result.Should().Be(OutputFormat.Human);
    }
}