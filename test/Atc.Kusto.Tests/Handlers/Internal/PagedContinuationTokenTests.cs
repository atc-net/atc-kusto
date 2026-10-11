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
    [InlineData("v2;abc;5;7")]
    [InlineData("v3;abc;5")]
    [InlineData("V2;abc;5")]
    [InlineData("")]
    public void Parse_Rejects_An_Invalid_Token(string token)
        => PagedContinuationToken.Parse(token).Should().BeNull();

    [Fact]
    public void Parse_Rejects_A_Nonce_Longer_Than_64_Characters()
    {
        // Act & Assert
        PagedContinuationToken.Parse($"v2;{new string('a', 64)};5").Should().NotBeNull();
        PagedContinuationToken.Parse($"v2;{new string('a', 65)};5").Should().BeNull();
    }

    [Theory]
    [InlineData("abc123", 1234567, "v2;abc123;1234567")]
    [InlineData(null, 40, "v2;;40")]
    public void ToString_Writes_The_Token_In_Any_Culture(
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
}