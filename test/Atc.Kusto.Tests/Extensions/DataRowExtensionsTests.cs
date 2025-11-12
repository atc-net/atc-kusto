namespace Atc.Kusto.Tests.Extensions;

public sealed class DataRowExtensionsTests
{
    public record TestObject(
        string Property1,
        string Property2,
        string Property3);

    public record TestObjectWithDecimal(
        string Name,
        decimal Amount);

    public record TestObjectWithNullableDecimal(
        string Name,
        decimal? Amount);

    public record TestObjectMixedTypes(
        string Name,
        int Count,
        decimal Amount,
        bool IsActive);

    public record TestObjectWithNestedObject(
        string Name,
        NestedDetails Details);

    public record NestedDetails(
        string Address,
        int ZipCode);

    public record TestObjectWithDynamicField(
        string Name,
        object? Metadata);

    public record TestObjectWithMixedTypesAndDynamic(
        string Name,
        int Count,
        object? DynamicData);

    [Fact]
    public void MapDataRow_Should_Return_Correct_Object()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Property1", typeof(string));
        dataTable.Columns.Add("Property2", typeof(string));
        dataTable.Columns.Add("Property3", typeof(string));

        var expected = new TestObject("Value1", "Value2", "Value3");

        var row = dataTable.NewRow();
        row["Property1"] = expected.Property1;
        row["Property2"] = expected.Property2;
        row["Property3"] = expected.Property3;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObject>();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void MapDataRow_Should_Throw_ArgumentNullException_When_Row_Is_Null()
    {
        // Arrange
        DataRow? nullRow = null;

        // Act
        Action act = () => nullRow!.MapDataRow<TestObject>();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapDataRow_Should_Handle_Decimal_Values()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Amount", typeof(decimal));

        var expected = new TestObjectWithDecimal("Test", 123.45m);

        var row = dataTable.NewRow();
        row["Name"] = expected.Name;
        row["Amount"] = expected.Amount;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithDecimal>();

        // Assert
        actual.Should().BeEquivalentTo(expected);
        actual!.Amount.Should().Be(123.45m);
    }

    [Fact]
    public void MapDataRow_Should_Handle_SqlDecimal_Values()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Amount", typeof(SqlDecimal));

        var expected = new TestObjectWithDecimal("Test", 456.78m);

        var row = dataTable.NewRow();
        row["Name"] = expected.Name;
        row["Amount"] = new SqlDecimal(expected.Amount);
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithDecimal>();

        // Assert
        actual.Should().NotBeNull();
        actual!.Name.Should().Be(expected.Name);
        actual.Amount.Should().Be(expected.Amount);
    }

    [Fact]
    public void MapDataRow_Should_Handle_DBNull_Values()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Amount", typeof(decimal));

        var expected = new TestObjectWithNullableDecimal("Test", null);

        var row = dataTable.NewRow();
        row["Name"] = expected.Name;
        row["Amount"] = DBNull.Value;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithNullableDecimal>();

        // Assert
        actual.Should().BeEquivalentTo(expected);
        actual!.Amount.Should().BeNull();
    }

    [Fact]
    public void MapDataRow_Should_Handle_Mixed_Types()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Count", typeof(int));
        dataTable.Columns.Add("Amount", typeof(SqlDecimal));
        dataTable.Columns.Add("IsActive", typeof(bool));

        var expected = new TestObjectMixedTypes("TestItem", 42, 999.99m, true);

        var row = dataTable.NewRow();
        row["Name"] = expected.Name;
        row["Count"] = expected.Count;
        row["Amount"] = new SqlDecimal(expected.Amount);
        row["IsActive"] = expected.IsActive;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectMixedTypes>();

        // Assert
        actual.Should().NotBeNull();
        actual!.Name.Should().Be(expected.Name);
        actual.Count.Should().Be(expected.Count);
        actual.Amount.Should().Be(expected.Amount);
        actual.IsActive.Should().Be(expected.IsActive);
    }

    [Fact]
    public void MapDataRow_Should_Handle_High_Precision_SqlDecimal()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Amount", typeof(SqlDecimal));

        // Create a SqlDecimal with high precision (28 digits, 10 scale)
        // Constructor: SqlDecimal(byte precision, byte scale, bool fPositive, int data1, int data2, int data3, int data4)
        var highPrecisionValue = new SqlDecimal(28, 10, true, 1234567890, 123456789, 12345, 0);
        var expectedDecimalValue = (decimal)highPrecisionValue;

        var row = dataTable.NewRow();
        row["Name"] = "HighPrecision";
        row["Amount"] = highPrecisionValue;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithDecimal>();

        // Assert
        actual.Should().NotBeNull();
        actual!.Name.Should().Be("HighPrecision");
        actual.Amount.Should().Be(expectedDecimalValue);
    }

    [Fact]
    public void MapDataRow_Should_Handle_Simple_JObject()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Metadata", typeof(object));

        const string metadataJson = """{"Key": "Value", "Number": 42}""";
        var metadataJToken = Newtonsoft.Json.Linq.JToken.Parse(metadataJson);

        var row = dataTable.NewRow();
        row["Name"] = "Test";
        row["Metadata"] = metadataJToken;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithDynamicField>();

        // Assert
        actual.Should().NotBeNull();
        actual!.Name.Should().Be("Test");
        actual.Metadata.Should().NotBeNull();

        var metadataElement = actual.Metadata as JsonElement?;
        metadataElement.Should().NotBeNull();
        metadataElement!.Value.GetProperty("Key").GetString().Should().Be("Value");
        metadataElement.Value.GetProperty("Number").GetInt32().Should().Be(42);
    }

    [Fact]
    public void MapDataRow_Should_Handle_Nested_JObject()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Details", typeof(object));

        const string detailsJson = """{"Address": "123 Main St", "ZipCode": 12345}""";
        var detailsJToken = Newtonsoft.Json.Linq.JToken.Parse(detailsJson);

        var row = dataTable.NewRow();
        row["Name"] = "John Doe";
        row["Details"] = detailsJToken;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithNestedObject>();

        // Assert
        actual.Should().NotBeNull();
        actual!.Name.Should().Be("John Doe");
        actual.Details.Should().NotBeNull();
        actual.Details.Address.Should().Be("123 Main St");
        actual.Details.ZipCode.Should().Be(12345);
    }

    [Fact]
    public void MapDataRow_Should_Handle_Mixed_Types_With_JToken()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Count", typeof(int));
        dataTable.Columns.Add("DynamicData", typeof(object));

        const string dynamicJson = """{"Status": "Active", "Tags": ["tag1", "tag2"]}""";
        var dynamicJToken = Newtonsoft.Json.Linq.JToken.Parse(dynamicJson);

        var row = dataTable.NewRow();
        row["Name"] = "TestItem";
        row["Count"] = 100;
        row["DynamicData"] = dynamicJToken;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithMixedTypesAndDynamic>();

        // Assert
        actual.Should().NotBeNull();
        actual!.Name.Should().Be("TestItem");
        actual.Count.Should().Be(100);
        actual.DynamicData.Should().NotBeNull();

        var dynamicElement = actual.DynamicData as JsonElement?;
        dynamicElement.Should().NotBeNull();
        dynamicElement!.Value.GetProperty("Status").GetString().Should().Be("Active");
    }

    [Fact]
    public void MapDataRow_Should_Handle_Complex_Nested_JObject()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Metadata", typeof(object));

        const string complexJson = """
                                   {
                                       "User": {
                                           "FirstName": "Jane",
                                           "LastName": "Smith"
                                       },
                                       "Settings": {
                                           "Theme": "Dark",
                                           "Notifications": true
                                       },
                                       "Scores": [95, 87, 92]
                                   }
                                   """;
        var complexJToken = Newtonsoft.Json.Linq.JToken.Parse(complexJson);

        var row = dataTable.NewRow();
        row["Name"] = "ComplexTest";
        row["Metadata"] = complexJToken;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithDynamicField>();

        // Assert
        actual.Should().NotBeNull();
        actual!.Name.Should().Be("ComplexTest");
        actual.Metadata.Should().NotBeNull();

        var metadataElement = actual.Metadata as JsonElement?;
        metadataElement.Should().NotBeNull();

        var userElement = metadataElement!.Value.GetProperty("User");
        userElement.GetProperty("FirstName").GetString().Should().Be("Jane");
        userElement.GetProperty("LastName").GetString().Should().Be("Smith");

        var settingsElement = metadataElement.Value.GetProperty("Settings");
        settingsElement.GetProperty("Theme").GetString().Should().Be("Dark");
        settingsElement.GetProperty("Notifications").GetBoolean().Should().BeTrue();

        var scoresArray = metadataElement.Value.GetProperty("Scores");
        scoresArray.GetArrayLength().Should().Be(3);
        scoresArray[0].GetInt32().Should().Be(95);
    }

    [Fact]
    public void MapDataRow_Should_Handle_JArray()
    {
        // Arrange
        using var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Metadata", typeof(object));

        const string arrayJson = """[{"Id": 1, "Value": "First"}, {"Id": 2, "Value": "Second"}]""";
        var arrayJToken = Newtonsoft.Json.Linq.JToken.Parse(arrayJson);

        var row = dataTable.NewRow();
        row["Name"] = "ArrayTest";
        row["Metadata"] = arrayJToken;
        dataTable.Rows.Add(row);

        // Act
        var actual = row.MapDataRow<TestObjectWithDynamicField>();

        // Assert
        actual.Should().NotBeNull();
        actual!.Name.Should().Be("ArrayTest");
        actual.Metadata.Should().NotBeNull();

        var metadataElement = actual.Metadata as JsonElement?;
        metadataElement.Should().NotBeNull();
        metadataElement!.Value.ValueKind.Should().Be(JsonValueKind.Array);
        metadataElement.Value.GetArrayLength().Should().Be(2);
        metadataElement.Value[0].GetProperty("Id").GetInt32().Should().Be(1);
        metadataElement.Value[0].GetProperty("Value").GetString().Should().Be("First");
    }
}