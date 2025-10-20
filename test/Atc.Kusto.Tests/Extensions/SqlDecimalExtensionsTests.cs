namespace Atc.Kusto.Tests.Extensions;

public sealed class SqlDecimalExtensionsTests
{
    [Theory]
    [InlineData(123.45)]
    [InlineData(0)]
    [InlineData(-456.78)]
    [InlineData(999999999.99)]
    [InlineData(0.01)]
    [InlineData(0.001)]
    [InlineData(0.0001)]
    public void ToDecimal_Should_Convert_Values_Correctly(decimal value)
    {
        // Arrange
        var sqlDecimal = new SqlDecimal(value);

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ToDecimal_Should_Handle_Maximum_Precision_Within_Limits()
    {
        // Arrange - Create a SqlDecimal with 28 digits precision, 10 scale (within .NET limits)
        var sqlDecimal = new SqlDecimal(28, 10, true, 1234567890, 123456789, 12345, 0);
        var expectedValue = (decimal)sqlDecimal;

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void ToDecimal_Should_Handle_Zero_Scale()
    {
        // Arrange - Integer value (no decimal places)
        var sqlDecimal = new SqlDecimal(10, 0, true, 1234567890, 0, 0, 0);
        var expectedValue = (decimal)sqlDecimal;

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void ToDecimal_Should_Handle_Maximum_Scale()
    {
        // Arrange - Maximum scale of 27
        const decimal value = 1.123456789012345678901234567m; // 27 decimal places
        var sqlDecimal = new SqlDecimal(value);

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ToDecimal_Should_Handle_Negative_Values()
    {
        // Arrange
        const decimal value = -123456.789m;
        var sqlDecimal = new SqlDecimal(value);

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ToDecimal_Should_Adjust_Scale_When_Total_Precision_Requires_It()
    {
        // Arrange - Create SqlDecimal with valid but high precision
        // 18 integer digits + 10 scale = 28 total (at limit, should convert directly)
        const decimal value = 123456789012345678.9876543210m;
        var sqlDecimal = new SqlDecimal(value);

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ToDecimal_Should_Throw_When_Value_Has_Too_Many_Integer_Digits()
    {
        // Arrange - SqlDecimal constructor validates, so we create a scenario
        // where conversion logic would detect the issue
        // We'll use Parse to create a SqlDecimal from a string that represents
        // a value with more integer digits than .NET decimal can handle
        const string bigValue = "123456789012345678901234567890.5"; // 30 integer digits
        var sqlDecimal = SqlDecimal.Parse(bigValue);

        // Act
        Action act = () => sqlDecimal.ToDecimal();

        // Assert
        act.Should().Throw<OverflowException>().WithMessage("*Integer part*exceeds*decimal capacity*");
    }

    [Fact]
    public void ToDecimal_Should_Handle_SqlDecimal_With_Low_Precision()
    {
        // Arrange - Very small precision
        var sqlDecimal = new SqlDecimal(5, 2, true, 12345, 0, 0, 0); // 123.45
        var expectedValue = (decimal)sqlDecimal;

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void ToDecimal_Should_Handle_SqlDecimal_With_High_Scale()
    {
        // Arrange - High scale relative to precision
        var sqlDecimal = new SqlDecimal(20, 18, true, 12345678, 0, 0, 0); // Very small number with many decimals

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().NotBe(0);
    }

    [Fact]
    public void ToDecimal_Should_Handle_Large_Positive_Values()
    {
        // Arrange - Large value that fits within .NET decimal constraints
        const decimal value = 999999999999999999999999.9999m; // 24 integer + 4 decimal = 28 total
        var sqlDecimal = new SqlDecimal(value);

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ToDecimal_Should_Handle_Large_Negative_Values()
    {
        // Arrange - Large negative value that fits within .NET decimal constraints
        const decimal value = -999999999999999999999999.9999m; // 24 integer + 4 decimal = 28 total
        var sqlDecimal = new SqlDecimal(value);

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ToDecimal_Should_Preserve_Precision_When_Within_Limits()
    {
        // Arrange - Value with 28 total digits (max for .NET decimal)
        const decimal value = 1234567890123456.7890123456m; // 16 integer + 10 decimal = 26 total
        var sqlDecimal = new SqlDecimal(value);

        // Act
        var result = sqlDecimal.ToDecimal();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ToDecimal_Should_Be_Idempotent()
    {
        // Arrange
        const decimal value = 12345.6789m;
        var sqlDecimal = new SqlDecimal(value);

        // Act
        var result1 = sqlDecimal.ToDecimal();
        var result2 = sqlDecimal.ToDecimal();

        // Assert
        result1.Should().Be(result2);
        result1.Should().Be(value);
    }
}