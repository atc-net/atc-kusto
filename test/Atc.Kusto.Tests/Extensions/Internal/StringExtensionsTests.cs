namespace Atc.Kusto.Tests.Extensions.Internal;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("abc123", "abc123")]
    [InlineData("abc!@#123", "abc123")]
    [InlineData("abc 123", "abc123")]
    [InlineData("!@#$%^&*()", "")]
    [InlineData("ABCdefGHI123", "ABCdefGHI123")]
    [InlineData("12345", "12345")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("a", "a")]
    [InlineData("Z", "Z")]
    [InlineData("a b c", "abc")]
    [InlineData("1!2@3#4$5%", "12345")]
    [InlineData("A1 B2 C3", "A1B2C3")]
    public void ToAlphaNumeric_ShouldRemoveNonAlphanumericCharacters(
        string input,
        string expected)
    {
        // Arrange & Act
        var actual = input.ToAlphaNumeric();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToAlphaNumeric_ShouldReturnEmptyString_WhenInputIsEmpty()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var actual = input.ToAlphaNumeric();

        // Assert
        Assert.Equal(string.Empty, actual);
    }
}