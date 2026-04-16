namespace Atc.Kusto.CLI.Tests.Rendering;

[Collection("ConsoleOutput")]
public sealed class TsvResultRendererTests
{
    [Fact]
    public void Render_TableOutput_WritesHeadersAndRows()
    {
        // Arrange
        var renderer = new TsvResultRenderer();
        var columns = new List<string> { "Name", "Count" };
        var rows = new List<string[]>
        {
            new[] { "alpha", "42" },
            new[] { "beta", "7" },
        };

        using var writer = new StringWriter();
        System.Console.SetOut(writer);

        // Act
        renderer.Render(columns, rows);

        // Assert
        var output = writer.ToString();
        var expected = $"Name\tCount{Environment.NewLine}alpha\t42{Environment.NewLine}beta\t7{Environment.NewLine}";
        output.Should().Be(expected);
    }

    [Fact]
    public void Render_EscapesTabsInValues()
    {
        // Arrange
        var renderer = new TsvResultRenderer();
        var columns = new List<string> { "Name", "Notes" };
        var rows = new List<string[]>
        {
            new[] { "alpha\tbeta", "simple" },
        };

        using var writer = new StringWriter();
        System.Console.SetOut(writer);

        // Act
        renderer.Render(columns, rows);

        // Assert
        var output = writer.ToString();
        output.Should().Contain("\"alpha\tbeta\"");
    }

    [Fact]
    public void Render_DoesNotEscapeCommas()
    {
        // Arrange
        var renderer = new TsvResultRenderer();
        var columns = new List<string> { "Name" };
        var rows = new List<string[]>
        {
            new[] { "alpha,beta" },
        };

        using var writer = new StringWriter();
        System.Console.SetOut(writer);

        // Act
        renderer.Render(columns, rows);

        // Assert
        var output = writer.ToString();
        output.Should().Contain("alpha,beta");
        output.Should().NotContain("\"alpha,beta\"");
    }

    [Fact]
    public void Render_EmptyRows_WritesOnlyHeader()
    {
        // Arrange
        var renderer = new TsvResultRenderer();
        var columns = new List<string> { "Name" };
        var rows = new List<string[]>();

        using var writer = new StringWriter();
        System.Console.SetOut(writer);

        // Act
        renderer.Render(columns, rows);

        // Assert
        var output = writer.ToString();
        output.Should().Be($"Name{Environment.NewLine}");
    }
}