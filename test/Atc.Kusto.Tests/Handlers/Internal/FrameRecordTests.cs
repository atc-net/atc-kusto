namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class FrameRecordTests
{
    [Fact]
    public void CreateRecord_Converts_Each_Value_To_Its_Column_Type()
    {
        // Arrange - progressive frames send the values as strings
        var table = CreateTable(
            ("id", typeof(Guid)),
            ("amount", typeof(SqlDecimal)),
            ("at", typeof(DateTime)),
            ("duration", typeof(TimeSpan)),
            ("name", typeof(string)),
            ("missing", typeof(int)));

        // Act
        var record = table.CreateRecord(
        [
            "74be27de-1e4e-49d9-b579-fe0b331d3642",
            "1.25",
            "2024-01-02T03:04:05.678Z",
            "01:00:00",
            "x",
            DBNull.Value,
        ]);

        // Assert
        record.FieldCount.Should().Be(6);
        record.GetGuid(0).Should().Be(Guid.Parse("74be27de-1e4e-49d9-b579-fe0b331d3642"));
        record.GetDecimal(1).Should().Be(1.25m);
        record.GetDateTime(2).Should().Be(new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc));
        record.GetValue(3).Should().Be(TimeSpan.FromHours(1));
        record.GetString(4).Should().Be("x");
        record.IsDBNull(5).Should().BeTrue();
        record["name"].Should().Be("x");
    }

    [Fact]
    public void CreateRecord_Copies_The_Values_So_The_Array_Can_Be_Reused()
    {
        // Arrange - the handler reuses one array for every row of a fragment
        var table = CreateTable(("name", typeof(string)));
        object?[] values = ["A"];

        // Act
        var first = table.CreateRecord(values);
        values[0] = "B";
        var second = table.CreateRecord(values);

        // Assert
        first.GetString(0).Should().Be("A");
        second.GetString(0).Should().Be("B");
    }

    [Fact]
    public void GetName_GetFieldType_And_GetDataTypeName_Return_The_Column_Name_And_Type()
    {
        // Arrange
        var record = CreateTable(("id", typeof(Guid)), ("count", typeof(long)))
            .CreateRecord(
            [
                Guid.Empty,
                5L,
            ]);

        // Assert
        record.GetName(1).Should().Be("count");
        record.GetFieldType(0).Should().Be<Guid>();
        record.GetDataTypeName(1).Should().Be(nameof(Int64));
    }

    [Fact]
    public void GetOrdinal_Prefers_An_Exact_Match_Then_Ignores_Case()
    {
        // Arrange
        var record = CreateTable(("Id", typeof(string)), ("id", typeof(string)))
            .CreateRecord(
            [
                "upper",
                "lower",
            ]);

        // Act & Assert
        record.GetOrdinal("id").Should().Be(1);
        record.GetOrdinal("Id").Should().Be(0);
        record.GetOrdinal("ID").Should().Be(0);
    }

    [Fact]
    public void GetOrdinal_Throws_For_An_Unknown_Column()
    {
        // Arrange
        var record = CreateTable(("id", typeof(string))).CreateRecord(["x"]);

        // Act
        var act = () => record.GetOrdinal("other");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*other*");
    }

    [Fact]
    public void GetValues_Copies_As_Many_Values_As_Fit()
    {
        // Arrange
        var record = CreateTable(("a", typeof(string)), ("b", typeof(string)))
            .CreateRecord(
            [
                "A",
                "B",
            ]);
        var values = new object[1];

        // Act
        var count = record.GetValues(values);

        // Assert
        count.Should().Be(1);
        values.Should().Equal("A");
    }

    private static FrameTable CreateTable(
        params (string Name, Type Type)[] columns)
    {
        using var schema = new DataTable(nameof(WellKnownDataSet.PrimaryResult));
        foreach (var (name, type) in columns)
        {
            schema.Columns.Add(name, type);
        }

        return FrameTable.FromSchemaFrame(new SchemaFrameStub(1, schema));
    }
}