namespace Atc.Kusto.Tests.Handlers.Internal;

public sealed class PagedContinuationTokenTests
{
    [Fact]
    public void Parse_Reads_A_Token_With_A_Nonce()
    {
        // Act
        var token = PagedContinuationToken.Parse("v2;abc123;40");

        // Assert
        token.Should().Be(new PagedContinuationToken("abc123", 40));
    }

    [Fact]
    public void Parse_Reads_A_Token_Without_A_Nonce()
    {
        // Act
        var token = PagedContinuationToken.Parse("v2;;40");

        // Assert
        token.Should().Be(new PagedContinuationToken(Nonce: null, 40));
    }

    [Theory]
    [InlineData("PartnerQueryabc;10")]
    [InlineData("v2;abc;-50")]
    [InlineData("v2;abc;+5")]
    [InlineData("v2;abc; 5")]
    [InlineData("v2;abc;5 ")]
    [InlineData("v2;abc;1.5")]
    [InlineData("v2;abc;1,000")]
    [InlineData("v2;abc;")]
    [InlineData("v2;abc;99999999999999999999")]
    [InlineData("v2;ab-c;5")]
    [InlineData("v2;æøå;5")]
    [InlineData("v2;abc;5;7;9")]
    [InlineData("v3;abc;5")]
    [InlineData("V2;abc;5")]
    [InlineData("")]
    public void Parse_Returns_Null_For_An_Invalid_Token(string token)
        => PagedContinuationToken.Parse(token).Should().BeNull();

    [Fact]
    public void Parse_Accepts_A_64_Character_Nonce_And_Returns_Null_For_A_Longer_One()
    {
        // Act & Assert
        PagedContinuationToken.Parse($"v2;{new string('a', 64)};5").Should().NotBeNull();
        PagedContinuationToken.Parse($"v2;{new string('a', 65)};5").Should().BeNull();
    }

    [Theory]
    [InlineData("abc123", 1234567, "v2;abc123;1234567")]
    [InlineData(null, 40, "v2;;40")]
    public void ToString_Writes_A_Token_That_Parse_Reads_Back_In_Any_Culture(
        string? nonce,
        long itemsReturned,
        string expected)
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");

        try
        {
            // Act
            var actual = new PagedContinuationToken(nonce, itemsReturned).ToString();

            // Assert
            actual.Should().Be(expected);
            PagedContinuationToken.Parse(actual).Should().Be(new PagedContinuationToken(nonce, itemsReturned));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_Reads_A_Token_With_A_Total_Count()
    {
        // Act
        var actual = PagedContinuationToken.Parse("v2;abc;1000;4701");

        // Assert
        actual.Should().Be(new PagedContinuationToken("abc", 1000, 4701));
    }

    [Theory]
    [InlineData("v2;abc;1000;-1")]
    [InlineData("v2;abc;1000;x")]
    [InlineData("v2;abc;1000;")]
    [InlineData("v2;abc;1000;1.5")]
    [InlineData("v2;abc;1000;4701;1")]
    public void Parse_Returns_Null_For_An_Invalid_Total_Count(string token)
        => PagedContinuationToken.Parse(token).Should().BeNull();

    [Fact]
    public void ToString_Writes_The_Total_Count_Last_When_It_Is_Known()
        => new PagedContinuationToken("abc", 1000, 4701).ToString().Should().Be("v2;abc;1000;4701");

    [Theory]
    [InlineData(3, 3, null, true)]
    [InlineData(2, 3, null, false)]
    [InlineData(0, 3, null, false)]
    [InlineData(3, 3, 10L, true)]
    [InlineData(3, 3, 6L, false)]
    [InlineData(3, 3, 5L, false)]
    public void CreateNext_Returns_A_Token_Only_When_The_Page_Is_Full_And_The_Total_Count_Is_Not_Reached(
        int pageItemCount,
        int pageSize,
        long? totalCount,
        bool expectsToken)
    {
        // Act - 6 items returned so far, including this page
        var actual = PagedContinuationToken.CreateNext(
            "abc",
            itemsReturned: 6,
            pageItemCount,
            pageSize,
            totalCount);

        // Assert
        if (expectsToken)
        {
            actual.Should().Be(new PagedContinuationToken("abc", 6, totalCount));
        }
        else
        {
            actual.Should().BeNull();
        }
    }
}