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
            pageSize: 1000,
            TimeSpan.FromHours(1));

        // Assert
        actual.Should().Be(
            ".set-or-replace stored_query_result ['DevicesQueryabc'] with (previewCount = 1000, expiresAfter = 3600s) <|\n" +
            "Devices | order by DeviceId asc\n" +
            "| serialize row_number = row_number()");
    }

    [Theory]
    [InlineData(90 * 60, "expiresAfter = 5400s")]
    [InlineData(1.9, "expiresAfter = 1s")]
    [InlineData(24 * 60 * 60, "expiresAfter = 86400s")]
    public void CreateSetOrReplace_Writes_The_Expiration_In_Whole_Seconds(
        double expirationSeconds,
        string expected)
    {
        // Arrange
        var query = Substitute.For<IKustoScript>();
        query.GetQueryText().Returns("Devices");

        // Act
        var actual = StoredQueryResultCommands.CreateSetOrReplace(
            query,
            "DevicesQueryabc",
            pageSize: 10,
            TimeSpan.FromSeconds(expirationSeconds));

        // Assert
        actual.Should().Contain(expected);
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