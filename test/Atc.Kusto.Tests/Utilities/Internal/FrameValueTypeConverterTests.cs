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
    public void ConvertToColumnType_Should_Handle_Invalid_String_For_SqlDecimal()
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
    [InlineData("3.14", typeof(double), 3.14)]
    [InlineData("true", typeof(bool), true)]
    public void ConvertToColumnType_Should_Use_ChangeType_For_Other_Conversions(
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
    public void ConvertToColumnType_Should_Handle_Numeric_Type_Conversions()
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
    public void ConvertToColumnType_Should_Use_InvariantCulture()
    {
        // Arrange - use a decimal string with period (not comma) as decimal separator
        const string value = "1234.56";
        var targetType = typeof(decimal);

        // Act
        var result = FrameValueTypeConverter.ConvertToColumnType(value, targetType);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<decimal>(result);
        Assert.Equal(1234.56m, result);
    }

    [Fact]
    public void ConvertToColumnType_Should_Handle_String_To_DateTime()
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
    public void ConvertToColumnType_Should_Handle_Realistic_Kusto_Decimal_Strings(
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