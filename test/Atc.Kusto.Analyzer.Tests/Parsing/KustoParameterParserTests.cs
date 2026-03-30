namespace Atc.Kusto.Analyzer.Tests.Parsing;

/// <summary>
/// Unit tests for <see cref="Atc.Kusto.Analyzer.Parsing.KustoParameterParser"/>.
/// </summary>
public sealed class KustoParameterParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t\n\r  ")]
    public void ParseParameters_EmptyOrNullInput_ReturnsEmptyList(
        string? kustoContent)
    {
        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseParameters_NoDeclaration_ReturnsEmptyList()
    {
        // Arrange
        const string kustoContent = """
            Customers
            | where customerId == CustomerKey
            | take 10
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseParameters_SingleParameter_WithoutDefault_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                customerId:long
            );
            Customers
            | where customerId == CustomerKey
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Single(result);
        Assert.Equal("customerId", result[0].Name);
        Assert.Equal("long", result[0].Type);
        Assert.False(result[0].HasDefaultValue);
    }

    [Fact]
    public void ParseParameters_SingleParameter_WithDefault_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                customerId:long = long(null)
            );
            Customers
            | where customerId == CustomerKey
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Single(result);
        Assert.Equal("customerId", result[0].Name);
        Assert.Equal("long", result[0].Type);
        Assert.True(result[0].HasDefaultValue);
    }

    [Fact]
    public void ParseParameters_TwoParameters_WithoutDefaults_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                customerId:long,
                name:string
            );
            Customers
            | where customerId == CustomerKey and name == FirstName
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("customerId", result[0].Name);
        Assert.Equal("long", result[0].Type);
        Assert.False(result[0].HasDefaultValue);
        Assert.Equal("name", result[1].Name);
        Assert.Equal("string", result[1].Type);
        Assert.False(result[1].HasDefaultValue);
    }

    [Fact]
    public void ParseParameters_ThreeParameters_ParsesInCorrectOrder()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                first:int,
                second:string,
                third:bool
            );
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("first", result[0].Name);
        Assert.Equal("second", result[1].Name);
        Assert.Equal("third", result[2].Name);
    }

    [Fact]
    public void ParseParameters_MixedDefaults_FirstWithoutSecondWith_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                customerId:long,
                name:string = ""
            );
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.False(result[0].HasDefaultValue);
        Assert.True(result[1].HasDefaultValue);
    }

    [Fact]
    public void ParseParameters_MixedDefaults_FirstWithSecondWithout_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                name:string = "",
                customerId:long
            );
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].HasDefaultValue);
        Assert.False(result[1].HasDefaultValue);
    }

    [Fact]
    public void ParseParameters_AllWithDefaults_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                customerId:long = long(null),
                name:string = "",
                age:int = 0
            );
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, param => Assert.True(param.HasDefaultValue));
    }

    [Theory]
    [InlineData("long")]
    [InlineData("int")]
    [InlineData("string")]
    [InlineData("datetime")]
    [InlineData("bool")]
    [InlineData("real")]
    [InlineData("guid")]
    [InlineData("timespan")]
    [InlineData("decimal")]
    public void ParseParameters_VariousTypes_ParsesCorrectly(string kustoType)
    {
        // Arrange
        var kustoContent = $"declare query_parameters (value:{kustoType});";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Single(result);
        Assert.Equal(kustoType, result[0].Type);
    }

    [Fact]
    public void ParseParameters_AllSupportedTypes_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = """
            declare query_parameters (
                p1:long,
                p2:int,
                p3:string,
                p4:datetime,
                p5:bool,
                p6:real,
                p7:guid,
                p8:timespan,
                p9:decimal
            );
            """;

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(9, result.Count);
        Assert.Equal("long", result[0].Type);
        Assert.Equal("int", result[1].Type);
        Assert.Equal("string", result[2].Type);
        Assert.Equal("datetime", result[3].Type);
        Assert.Equal("bool", result[4].Type);
        Assert.Equal("real", result[5].Type);
        Assert.Equal("guid", result[6].Type);
        Assert.Equal("timespan", result[7].Type);
        Assert.Equal("decimal", result[8].Type);
    }

    [Theory]
    [InlineData("name:string = \"\"")]
    [InlineData("name:string = \"test\"")]
    [InlineData("value:long = 0")]
    [InlineData("value:long = 100")]
    [InlineData("value:long = -100")]
    [InlineData("value:long = long(null)")]
    [InlineData("value:datetime = datetime(null)")]
    public void ParseParameters_DefaultValues_SetsHasDefaultValueTrue(
        string parameterDeclaration)
    {
        // Arrange
        var kustoContent = $"declare query_parameters ({parameterDeclaration});";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].HasDefaultValue);
    }

    [Theory]
    [InlineData("declare query_parameters(customerId:long);")]
    [InlineData("declare query_parameters (customerId  :  long);")]
    [InlineData("declare\tquery_parameters\t(customerId:long);")]
    public void ParseParameters_WhitespaceVariations_ParsesCorrectly(
        string kustoContent)
    {
        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Single(result);
        Assert.Equal("customerId", result[0].Name);
        Assert.Equal("long", result[0].Type);
    }

    [Fact]
    public void ParseParameters_ExtraSpacesAroundComma_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = "declare query_parameters (id:long  ,  name:string);";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("id", result[0].Name);
        Assert.Equal("name", result[1].Name);
    }

    [Theory]
    [InlineData("DECLARE QUERY_PARAMETERS (customerId:long);")]
    [InlineData("DeCLaRe QuErY_PaRaMeTeRs (customerId:long);")]
    public void ParseParameters_DeclareKeywordCaseInsensitive_ParsesCorrectly(
        string kustoContent)
    {
        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Single(result);
        Assert.Equal("customerId", result[0].Name);
    }

    [Theory]
    [InlineData("customerId:LONG", "LONG")]
    [InlineData("value:LoNg", "LoNg")]
    public void ParseParameters_TypeCasePreserved_ParsesCorrectly(
        string parameterDeclaration,
        string expectedType)
    {
        // Arrange
        var kustoContent = $"declare query_parameters ({parameterDeclaration});";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedType, result[0].Type);
    }

    [Theory]
    [InlineData("customerId:long", "customerId")]
    [InlineData("CustomerId:long", "CustomerId")]
    [InlineData("param_name:long", "param_name")]
    public void ParseParameters_NamingConventions_PreservesCase(
        string parameterDeclaration,
        string expectedName)
    {
        // Arrange
        var kustoContent = $"declare query_parameters ({parameterDeclaration});";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedName, result[0].Name);
    }

    [Fact]
    public void ParseParameters_NameWithNumbers_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = "declare query_parameters (param1:long, param2:string);";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("param1", result[0].Name);
        Assert.Equal("param2", result[1].Name);
    }

    [Fact]
    public void ParseParameters_SingleLetterName_ParsesCorrectly()
    {
        // Arrange
        const string kustoContent = "declare query_parameters (x:long, y:int, z:string);";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("x", result[0].Name);
        Assert.Equal("y", result[1].Name);
        Assert.Equal("z", result[2].Name);
    }

    [Fact]
    public void ParseParameters_EmptyParameterList_ReturnsEmptyList()
    {
        // Arrange
        const string kustoContent = "declare query_parameters ();";

        // Act
        var result = Atc.Kusto.Analyzer.Parsing.KustoParameterParser.ParseParameters(kustoContent);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}