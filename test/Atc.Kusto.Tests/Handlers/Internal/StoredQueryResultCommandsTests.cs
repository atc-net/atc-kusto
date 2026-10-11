namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class StoredQueryResultCommandsTests
{
    [Fact]
    public void CreateSetOrReplace_Stores_The_Query_Result_With_Numbered_Rows()
    {
        // Arrange
        var query = Substitute.For<IKustoScript>();
        query.GetQueryText().Returns("  Devices | order by DeviceId asc;\n");

        // Act
        var actual = StoredQueryResultCommands.CreateSetOrReplace(
            query,
            "DevicesQueryabc",
            pageSize: 1000);

        // Assert
        actual.Should().Be(
            ".set-or-replace stored_query_result ['DevicesQueryabc'] with (previewCount = 1000, expiresAfter = 1h) <|\n" +
            "Devices | order by DeviceId asc\n" +
            "| serialize row_number = row_number()");
    }

    [Theory]
    [InlineData(0, 1000, "between(1 .. 1000)")]
    [InlineData(1000, 1000, "between(1001 .. 2000)")]
    [InlineData(5_000_000_000, 10, "between(5000000001 .. 5000000010)")]
    public void CreatePageQuery_Selects_The_Row_Numbers_Of_The_Next_Page(
        long itemsReturned,
        int pageSize,
        string expectedRange)
    {
        // Act
        var actual = StoredQueryResultCommands.CreatePageQuery(
            "DevicesQueryabc",
            itemsReturned,
            pageSize);

        // Assert
        actual.Should().Be($"stored_query_result('DevicesQueryabc') | where row_number {expectedRange}");
    }
}