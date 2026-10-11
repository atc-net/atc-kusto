namespace Atc.Kusto.Tests;

public sealed class KustoQueryReadResultTests
{
    [Fact]
    public void ReadResult_Maps_The_Rows_With_The_SerializerOptions_It_Receives()
    {
        // Arrange - a lower-case column and a PascalCase property
        var caseSensitive = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
        var caseInsensitive = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        using var firstReader = CreateReader();
        using var secondReader = CreateReader();

        // Act
        var withCaseSensitive = new PersonsQuery().ReadResult(firstReader, caseSensitive);
        var withCaseInsensitive = new PersonsQuery().ReadResult(secondReader, caseInsensitive);

        // Assert
        withCaseSensitive!.Single().Name.Should().BeNull();
        withCaseInsensitive!.Single().Name.Should().Be("Ada");
    }

    private static DataTableReader CreateReader()
    {
        using var table = new DataTable();
        table.Columns.Add("name", typeof(string));
        table.Rows.Add("Ada");

        return table.CreateDataReader();
    }

    private sealed record Person(string? Name);

    private sealed record PersonsQuery : KustoQuery<Person>
    {
        public override string GetQueryText() => "Persons";
    }
}