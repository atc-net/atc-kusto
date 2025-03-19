namespace Atc.Kusto.Tests.Extensions;

public sealed class DataRowExtensionsTests
{
    public record TestObject(
        string Property1,
        string Property2,
        string Property3);

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
}