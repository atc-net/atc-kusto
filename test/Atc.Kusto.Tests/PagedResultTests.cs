namespace Atc.Kusto.Tests;

public sealed class PagedResultTests
{
    [Fact]
    public void Deconstructs_Into_Items_And_ContinuationToken()
    {
        // Arrange
        var result = new PagedResult<string>(["A"], "v2;;1")
        {
            TotalCount = 5,
        };

        // Act
        var (items, continuationToken) = result;

        // Assert
        items.Should().Equal("A");
        continuationToken.Should().Be("v2;;1");
    }

    [Fact]
    public void TotalCount_Is_Null_When_Not_Set()
        => new PagedResult<string>([], ContinuationToken: null).TotalCount.Should().BeNull();
}