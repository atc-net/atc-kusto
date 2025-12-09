namespace Atc.Kusto.Analyzer.Tests.Parsing;

/// <summary>
/// Unit tests for <see cref="Atc.Kusto.Analyzer.Parsing.KustoProjectionParser"/>.
/// </summary>
public sealed class KustoProjectionParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t\n\r  ")]
    public void ParseFinalProjection_EmptyOrNullInput_ReturnsNull(string? kustoContent)
    {
        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFinalProjection_NoProjectStatement_ReturnsNull()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | where customerId == CustomerKey
            | take 10
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFinalProjection_SingleField_ReturnsFieldName()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project CustomerKey
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("CustomerKey", result[0]);
    }

    [Fact]
    public void ParseFinalProjection_MultipleFields_ReturnsAllFieldNames()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project CustomerKey, FirstName, LastName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FirstName", result[1]);
        Assert.Equal("LastName", result[2]);
    }

    [Fact]
    public void ParseFinalProjection_MultiLineProjection_ReturnsAllFieldNames()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project
                CustomerKey,
                FirstName,
                LastName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FirstName", result[1]);
        Assert.Equal("LastName", result[2]);
    }

    [Fact]
    public void ParseFinalProjection_AliasedField_ReturnsAliasName()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project FullName = strcat(FirstName, " ", LastName)
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("FullName", result[0]);
    }

    [Fact]
    public void ParseFinalProjection_MixedAliasedAndSimpleFields_ReturnsAllNames()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project
                CustomerKey,
                FullName = strcat(FirstName, " ", LastName),
                Age
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FullName", result[1]);
        Assert.Equal("Age", result[2]);
    }

    [Fact]
    public void ParseFinalProjection_NonFinalProject_ReturnsNull()
    {
        // Arrange - project is NOT the last pipe operation
        const string kustoContent = """
            Customers
            | project CustomerKey, FirstName, LastName
            | take 10
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFinalProjection_ProjectFollowedByWhere_ReturnsNull()
    {
        // Arrange - project is NOT the last pipe operation
        const string kustoContent = """
            Customers
            | project CustomerKey, FirstName, LastName
            | where CustomerKey > 100
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseFinalProjection_MultipleProjects_UsesLastOne()
    {
        // Arrange - only the LAST project should be considered
        const string kustoContent = """
            Customers
            | project CustomerKey, FirstName, LastName, Email
            | where CustomerKey > 100
            | project CustomerKey, LastName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("LastName", result[1]);
    }

    [Fact]
    public void ParseFinalProjection_SingleLineComment_IgnoresCommentedCode()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project
                CustomerKey,
                // Email,
                FirstName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FirstName", result[1]);
    }

    [Fact]
    public void ParseFinalProjection_MultiLineComment_IgnoresCommentedCode()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project
                CustomerKey,
                /* Email,
                   PhoneNumber, */
                FirstName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FirstName", result[1]);
    }

    [Fact]
    public void ParseFinalProjection_NestedParentheses_HandlesCorrectly()
    {
        // Arrange - function calls with nested parentheses
        const string kustoContent = """
            Customers
            | project
                CustomerKey,
                FullName = strcat(FirstName, " ", coalesce(MiddleName, ""), " ", LastName)
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FullName", result[1]);
    }

    [Theory]
    [InlineData("| PROJECT CustomerKey")]
    [InlineData("| Project CustomerKey")]
    [InlineData("| pRoJeCt CustomerKey")]
    public void ParseFinalProjection_CaseInsensitive_ParsesCorrectly(string projectStatement)
    {
        // Arrange
        var kustoContent = $"Customers\n{projectStatement}";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("CustomerKey", result[0]);
    }

    [Fact]
    public void ParseFinalProjection_ExtraWhitespace_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            Customers
            |   project    CustomerKey  ,   FirstName   ,  LastName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FirstName", result[1]);
        Assert.Equal("LastName", result[2]);
    }

    [Fact]
    public void ParseFinalProjection_WithDeclareParameters_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                customerId:long = long(null)
            );
            Customers
            | where isnull(customerId) or customerId == CustomerKey
            | project CustomerKey, FirstName, LastName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FirstName", result[1]);
        Assert.Equal("LastName", result[2]);
    }

    [Fact]
    public void ParseFinalProjection_EmptyProjection_ReturnsNull()
    {
        // Arrange - edge case: project with no fields is not a valid projection
        const string kustoContent = """
            Customers
            | project
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert - no fields means no valid projection pattern match
        Assert.Null(result);
    }

    [Fact]
    public void ParseFinalProjection_FieldNamesPreserveCase()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project customerKey, FIRSTNAME, lastName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("customerKey", result[0]);
        Assert.Equal("FIRSTNAME", result[1]);
        Assert.Equal("lastName", result[2]);
    }

    [Fact]
    public void ParseFinalProjection_FieldWithNumbers_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | project Field1, Field2, Field3
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Field1", result[0]);
        Assert.Equal("Field2", result[1]);
        Assert.Equal("Field3", result[2]);
    }

    [Fact]
    public void ParseFinalProjection_ComplexExpression_ExtractsAliasName()
    {
        // Arrange - complex expression with multiple operators
        const string kustoContent = """
            Customers
            | project
                CustomerKey,
                TotalValue = (OrderCount * AverageOrder) + Bonus,
                IsActive = Active == true and LastLogin > ago(30d)
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("TotalValue", result[1]);
        Assert.Equal("IsActive", result[2]);
    }

    [Fact]
    public void ParseFinalProjection_TrailingComma_HandlesGracefully()
    {
        // Arrange - trailing comma (common copy-paste issue)
        const string kustoContent = """
            Customers
            | project CustomerKey, FirstName, LastName,
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoProjectionParser.ParseFinalProjection(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("CustomerKey", result[0]);
        Assert.Equal("FirstName", result[1]);
        Assert.Equal("LastName", result[2]);
    }
}