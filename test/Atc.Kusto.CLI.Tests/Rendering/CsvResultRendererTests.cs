namespace Atc.Kusto.CLI.Tests.Rendering;

public sealed class CsvResultRendererTests
{
    [Fact]
    public void Render_TableOutput_WritesHeadersAndRows()
    {
        // Arrange
        var renderer = new CsvResultRenderer();
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
        var expected = $"Name,Count{Environment.NewLine}alpha,42{Environment.NewLine}beta,7{Environment.NewLine}";
        output.Should().Be(expected);
    }

    [Fact]
    public void Render_EscapesSpecialCharacters()
    {
        // Arrange
        var renderer = new CsvResultRenderer();
        var columns = new List<string> { "Name", "Notes", "Quote" };
        var rows = new List<string[]>
        {
            new[] { "alpha,beta", $"line1{Environment.NewLine}line2", "he said \"hi\"" },
        };

        using var writer = new StringWriter();
        System.Console.SetOut(writer);

        // Act
        renderer.Render(columns, rows);

        // Assert
        var output = writer.ToString();
        output.Should().Contain("\"alpha,beta\"");
        output.Should().Contain("\"he said \"\"hi\"\"\"");
    }

    [Fact]
    public void Render_EmptyRows_WritesOnlyHeader()
    {
        // Arrange
        var renderer = new CsvResultRenderer();
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

    [Fact]
    public void RenderStatistics_WritesStatisticCsv()
    {
        // Arrange
        var renderer = new CsvResultRenderer();
        var stats = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ExecutionTimeSec"] = "1.23",
            ["Cpu.Total"] = "00:00:01.5",
        };

        using var writer = new StringWriter();
        System.Console.SetOut(writer);

        // Act
        renderer.RenderStatistics(stats);

        // Assert
        var output = writer.ToString();
        output.Should().Contain("Statistic,Value");
        output.Should().Contain("ExecutionTimeSec,1.23");
        output.Should().Contain("Cpu.Total,00:00:01.5");
    }
}