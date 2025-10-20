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
}