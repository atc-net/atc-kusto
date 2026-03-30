namespace Atc.Kusto.CLI.Tests.Models;

public sealed class QueryFileReferenceParserTests
{
    [Fact]
    public void Parse_SimplePath_ReturnsPathWithoutRange()
    {
        // Act
        var result = QueryFileReferenceParser.Parse("query.kql");

        // Assert
        result.Path.Should().Be("query.kql");
        result.LineRange.Should().BeNull();
    }

    [Fact]
    public void Parse_WindowsAbsolutePath_ReturnsPathWithoutRange()
    {
        // Act
        var result = QueryFileReferenceParser.Parse(@"C:\queries\top-states.kql");

        // Assert
        result.Path.Should().Be(@"C:\queries\top-states.kql");
        result.LineRange.Should().BeNull();
    }

    [Fact]
    public void Parse_PathWithLineRange_ReturnsPathAndRange()
    {
        // Act
        var result = QueryFileReferenceParser.Parse(@"C:\queries\top-states.kql:12-15");

        // Assert
        result.Path.Should().Be(@"C:\queries\top-states.kql");
        result.LineRange.Should().NotBeNull();
        result.LineRange!.Value.StartLine.Should().Be(12);
        result.LineRange.Value.EndLine.Should().Be(15);
    }

    [Fact]
    public void Parse_RelativePathWithRange_ReturnsPathAndRange()
    {
        // Act
        var result = QueryFileReferenceParser.Parse("query.kql:5-10");

        // Assert
        result.Path.Should().Be("query.kql");
        result.LineRange.Should().NotBeNull();
        result.LineRange!.Value.StartLine.Should().Be(5);
        result.LineRange.Value.EndLine.Should().Be(10);
    }

    [Fact]
    public void Parse_SingleLineRange_ReturnsRangeWithSameStartAndEnd()
    {
        // Act
        var result = QueryFileReferenceParser.Parse("query.kql:3-3");

        // Assert
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
        // Act
        var act = () => QueryFileReferenceParser.Parse(fileReference);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*invalid*");
    }

    [Theory]
    [InlineData("query.kql:0-1")]
    [InlineData("query.kql:-1-2")]
    public void Parse_NonPositiveLineNumbers_ThrowsArgumentException(
        string fileReference)
    {
        // Act
        var act = () => QueryFileReferenceParser.Parse(fileReference);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive integers*");
    }

    [Fact]
    public void Parse_EndBeforeStart_ThrowsArgumentException()
    {
        // Act
        var act = () => QueryFileReferenceParser.Parse("query.kql:5-2");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*end line must be greater than or equal*");
    }

    [Fact]
    public void Parse_EmptyPath_ThrowsArgumentException()
    {
        // Act
        var act = () => QueryFileReferenceParser.Parse(":1-5");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }
}