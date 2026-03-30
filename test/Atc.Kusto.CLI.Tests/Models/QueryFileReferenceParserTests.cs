namespace Atc.Kusto.CLI.Tests.Models;

public sealed class QueryFileReferenceParserTests
{
    [Fact]
    public void Parse_SimplePath_ReturnsPathWithoutRange()
    {
        var result = QueryFileReferenceParser.Parse("query.kql");

        result.Path.Should().Be("query.kql");
        result.LineRange.Should().BeNull();
    }

    [Fact]
    public void Parse_WindowsAbsolutePath_ReturnsPathWithoutRange()
    {
        var result = QueryFileReferenceParser.Parse(@"C:\queries\top-states.kql");

        result.Path.Should().Be(@"C:\queries\top-states.kql");
        result.LineRange.Should().BeNull();
    }

    [Fact]
    public void Parse_PathWithLineRange_ReturnsPathAndRange()
    {
        var result = QueryFileReferenceParser.Parse(@"C:\queries\top-states.kql:12-15");

        result.Path.Should().Be(@"C:\queries\top-states.kql");
        result.LineRange.Should().NotBeNull();
        result.LineRange!.Value.StartLine.Should().Be(12);
        result.LineRange.Value.EndLine.Should().Be(15);
    }

    [Fact]
    public void Parse_RelativePathWithRange_ReturnsPathAndRange()
    {
        var result = QueryFileReferenceParser.Parse("query.kql:5-10");

        result.Path.Should().Be("query.kql");
        result.LineRange.Should().NotBeNull();
        result.LineRange!.Value.StartLine.Should().Be(5);
        result.LineRange.Value.EndLine.Should().Be(10);
    }

    [Fact]
    public void Parse_SingleLineRange_ReturnsRangeWithSameStartAndEnd()
    {
        var result = QueryFileReferenceParser.Parse("query.kql:3-3");

        result.LineRange.Should().NotBeNull();
        result.LineRange!.Value.StartLine.Should().Be(3);
        result.LineRange.Value.EndLine.Should().Be(3);
        result.LineRange.Value.LineCount.Should().Be(1);
    }

    [Theory]
    [InlineData("query.kql:1")]
    [InlineData("query.kql:1-")]
    [InlineData("query.kql:1-a")]
    public void Parse_InvalidRangeSyntax_ThrowsArgumentException(
        string fileReference)
    {
        var act = () => QueryFileReferenceParser.Parse(fileReference);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*invalid*");
    }

    [Theory]
    [InlineData("query.kql:0-1")]
    [InlineData("query.kql:-1-2")]
    public void Parse_NonPositiveLineNumbers_ThrowsArgumentException(
        string fileReference)
    {
        var act = () => QueryFileReferenceParser.Parse(fileReference);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive integers*");
    }

    [Fact]
    public void Parse_EndBeforeStart_ThrowsArgumentException()
    {
        var act = () => QueryFileReferenceParser.Parse("query.kql:5-2");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*end line must be greater than or equal*");
    }

    [Fact]
    public void Parse_EmptyPath_ThrowsArgumentException()
    {
        var act = () => QueryFileReferenceParser.Parse(":1-5");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }
}