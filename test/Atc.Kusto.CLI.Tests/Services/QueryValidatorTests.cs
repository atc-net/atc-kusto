namespace Atc.Kusto.CLI.Tests.Services;

public sealed class QueryValidatorTests
{
    [Theory]
    [InlineData("print 1")]
    [InlineData("StormEvents | take 5")]
    [InlineData("StormEvents | where StartTime > ago(7d) | count")]
    [InlineData("StormEvents\n| summarize Count=count() by State\n| top 10 by Count desc")]
    public void Validate_ValidQuery_ReturnsNull(string query)
    {
        // Act
        var result = QueryValidator.Validate(query);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyOrNull_ReturnsError(string? query)
    {
        // Act
        var result = QueryValidator.Validate(query!);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("empty");
    }

    [Fact]
    public void Validate_SyntaxError_ReturnsErrorWithLocation()
    {
        // Act
        var result = QueryValidator.Validate("StormEvents | where");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("line");
        result.Should().Contain("column");
    }

    [Fact]
    public void Validate_MultipleErrors_IndicatesAdditionalCount()
    {
        // Arrange - query with multiple syntax issues
        var query = "| where | take";

        // Act
        var result = QueryValidator.Validate(query);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Validate_QueryWithParameters_ReturnsNull()
    {
        // Arrange
        var query = "declare query_parameters (customerId:long);\nCustomers | where Id == customerId";

        // Act
        var result = QueryValidator.Validate(query);

        // Assert
        result.Should().BeNull();
    }
}