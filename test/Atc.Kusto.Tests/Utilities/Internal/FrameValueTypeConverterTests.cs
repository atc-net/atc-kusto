namespace Atc.Kusto.Tests.Utilities.Internal;

public sealed class FrameValueTypeConverterTests
{
    [Fact]
    public void ConvertToColumnType_Should_Return_DBNull_When_Value_Is_Null()
    {
        // Arrange
        var targetType = typeof(string);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(null, targetType);

        // Assert
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public void ConvertToColumnType_Should_Return_DBNull_When_Value_Is_DBNull()
    {
        // Arrange
        object? value = DBNull.Value;
        var targetType = typeof(int);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(value, targetType);

        // Assert
        Assert.Equal(DBNull.Value, result);
    }

    [Fact]
    public void ConvertToColumnType_Should_Return_Same_Value_When_Types_Match()
    {
        // Arrange
        const int value = 42;
        var targetType = typeof(int);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(value, targetType);

        // Assert
        Assert.Equal(42, result);
        Assert.IsType<int>(result);
    }

    [Theory]
    [InlineData("123.45", 123.45)]
    [InlineData("0.001", 0.001)]
    [InlineData("999999.99", 999999.99)]
    [InlineData("-456.78", -456.78)]
    public void ConvertToColumnType_Should_Convert_String_To_SqlDecimal(
        string stringValue,
        decimal expectedDecimalValue)
    {
        // Arrange
        var targetType = typeof(SqlDecimal);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(stringValue, targetType);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<SqlDecimal>(result);
        var sqlDecimal = (SqlDecimal)result;
        Assert.Equal(expectedDecimalValue, (decimal)sqlDecimal);
    }

    [Fact]
    public void ConvertToColumnType_Should_Return_The_Original_String_When_It_Is_Not_A_Decimal()
    {
        // Arrange
        const string value = "not-a-number";
        var targetType = typeof(SqlDecimal);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(value, targetType);

        // Assert - should return original value when conversion fails
        Assert.Equal("not-a-number", result);
    }

    [Theory]
    [InlineData("42", typeof(int), 42)]
    [InlineData("true", typeof(bool), true)]
    public void ConvertToColumnType_Should_Convert_Strings_To_Int_And_Bool(
        string stringValue,
        Type targetType,
        object expectedValue)
    {
        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(stringValue, targetType);

        // Assert
        Assert.NotNull(result);
        Assert.IsType(targetType, result);
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void ConvertToColumnType_Should_Return_Original_Value_When_Conversion_Fails()
    {
        // Arrange - try to convert a complex object to an int
        var value = new { Property = "test" };
        var targetType = typeof(int);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(value, targetType);

        // Assert - should return original value
        Assert.Equal(value, result);
    }

    [Fact]
    public void ConvertToColumnType_Should_Convert_An_Int_To_A_Long()
    {
        // Arrange
        const int value = 42;
        var targetType = typeof(long);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(value, targetType);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<long>(result);
        Assert.Equal(42L, result);
    }

    [Fact]
    public void ConvertToColumnType_Should_Read_A_Decimal_With_The_Invariant_Culture()
    {
        // Arrange - a period as decimal separator, on a culture that uses a comma
        const string value = "1234.56";
        var targetType = typeof(decimal);
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");

        try
        {
            // Act
            var result = FrameValueTypeConverter.ConvertToColumnType(value, targetType);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<decimal>(result);
            Assert.Equal(1234.56m, result);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("da-DK", 2.5)]
    [InlineData("da-DK", 1234.5)]
    [InlineData("da-DK", -0.75)]
    [InlineData("de-DE", 1234.5)]
    [InlineData("sv-SE", -0.75)]
    [InlineData("en-US", 1234.5)]
    public void ConvertToColumnType_Reads_A_Real_Written_With_The_Current_Culture(
        string culture,
        double expected)
    {
        // Arrange - the SDK writes real values in progressive frames with the current culture ("2,5" on da-DK)
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

        try
        {
            var value = expected.ToString(CultureInfo.CurrentCulture);

            // Act
            var result = FrameValueTypeConverter.ConvertToColumnType(value, typeof(double));

            // Assert
            Assert.Equal(expected, result);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ConvertToColumnType_Converts_A_String_To_A_Guid()
    {
        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType("74be27de-1e4e-49d9-b579-fe0b331d3642", typeof(Guid));

        // Assert
        Assert.Equal(Guid.Parse("74be27de-1e4e-49d9-b579-fe0b331d3642"), result);
    }

    [Theory]
    [InlineData("01:00:00", 0, 1, 0, 0, 0)]
    [InlineData("1.02:03:04.5000000", 1, 2, 3, 4, 500)]
    [InlineData("-00:00:01", 0, 0, 0, -1, 0)]
    public void ConvertToColumnType_Converts_A_String_To_A_TimeSpan(
        string value,
        int days,
        int hours,
        int minutes,
        int seconds,
        int milliseconds)
    {
        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(value, typeof(TimeSpan));

        // Assert
        Assert.Equal(new TimeSpan(days, hours, minutes, seconds, milliseconds), result);
    }

    [Fact]
    public void ConvertToColumnType_Keeps_A_Kusto_DateTime_In_Utc()
    {
        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType("2024-01-02T03:04:05.678Z", typeof(DateTime));

        // Assert
        var dateTime = Assert.IsType<DateTime>(result);
        Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc), dateTime);
    }

    [Fact]
    public void ConvertToColumnType_Should_Convert_A_String_To_A_DateTime()
    {
        // Arrange
        const string value = "2024-01-15T10:30:00Z";
        var targetType = typeof(DateTime);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(value, targetType);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DateTime>(result);
    }

    [Theory]
    [InlineData("20443.07")]
    [InlineData("12345.6789")]
    [InlineData("0.00")]
    public void ConvertToColumnType_Should_Convert_Kusto_Decimal_Strings_To_SqlDecimal(
        string decimalString)
    {
        // Arrange - this simulates the actual scenario from the Kusto progressive frames
        var targetType = typeof(SqlDecimal);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(decimalString, targetType);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<SqlDecimal>(result);

        var sqlDecimal = (SqlDecimal)result;
        var expectedDecimal = decimal.Parse(decimalString, CultureInfo.InvariantCulture);
        Assert.Equal(expectedDecimal, (decimal)sqlDecimal);
    }
}