namespace Atc.Kusto.CLI.Tests.Rendering;

public sealed class DisplayValueFormatterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void FormatCellValue_NullOrEmpty_ReturnsEmpty(
        string? input,
        string expected)
    {
        DisplayValueFormatter.FormatCellValue(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("42", "42")]
    [InlineData("1000", "1,000")]
    [InlineData("66380993", "66,380,993")]
    [InlineData("-1234567", "-1,234,567")]
    [InlineData("0", "0")]
    public void FormatCellValue_Integers_AddsThousandSeparators(
        string input,
        string expected)
    {
        DisplayValueFormatter.FormatCellValue(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("3.14", "3.14")]
    [InlineData("1234.56", "1,234.56")]
    [InlineData("-9876.543", "-9,876.543")]
    [InlineData("1000000.1", "1,000,000.1")]
    public void FormatCellValue_Decimals_AddsThousandSeparatorsAndPreservesDecimalPlaces(
        string input,
        string expected)
    {
        DisplayValueFormatter.FormatCellValue(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("001234")]
    [InlineData("007")]
    [InlineData("-001234")]
    public void FormatCellValue_ZeroPadded_PreservedAsIdentifiers(string input)
    {
        DisplayValueFormatter.FormatCellValue(input).Should().Be(input);
    }

    [Theory]
    [InlineData("2026-02-27T00:00:00Z", "2026-02-27")]
    [InlineData("2026-02-27T00:00:00+00:00", "2026-02-27")]
    public void FormatCellValue_MidnightDatetime_StripsTime(
        string input,
        string expected)
    {
        DisplayValueFormatter.FormatCellValue(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("2026-02-27T14:30:00Z", "2026-02-27 14:30:00Z")]
    [InlineData("2026-02-27T14:30:00+05:00", "2026-02-27 14:30:00+05:00")]
    [InlineData("2026-02-27T08:15:30.123Z", "2026-02-27 08:15:30.123Z")]
    public void FormatCellValue_NonMidnightDatetime_ReplacesTWithSpace(
        string input,
        string expected)
    {
        DisplayValueFormatter.FormatCellValue(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("some text with spaces")]
    [InlineData("2026-02-27")]
    public void FormatCellValue_NonNumericNonDatetime_ReturnedAsIs(string input)
    {
        DisplayValueFormatter.FormatCellValue(input).Should().Be(input);
    }

    [Theory]
    [InlineData("42", true)]
    [InlineData("3.14", true)]
    [InlineData("-100", true)]
    [InlineData("hello", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsNumericValue_ReturnsExpected(
        string? input,
        bool expected)
    {
        DisplayValueFormatter.IsNumericValue(input).Should().Be(expected);
    }
}