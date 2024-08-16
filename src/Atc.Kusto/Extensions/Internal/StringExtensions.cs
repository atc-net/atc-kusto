namespace Atc.Kusto.Extensions.Internal;

/// <summary>
/// Provides extension methods for string processing within the Kusto domain,
/// including methods to filter strings to alphanumeric characters only.
/// </summary>
internal static partial class StringExtensions
{
    /// <summary>
    /// A source-generated regular expression used to filter out non-alphanumeric characters from a string.
    /// The regular expression matches any character that is not a letter (a-z, A-Z) or a digit (0-9).
    /// </summary>
    /// <returns>A <see cref="Regex"/> instance that can be used to replace non-alphanumeric characters.</returns>

    [GeneratedRegex("[^a-zA-Z0-9]", RegexOptions.Singleline, 1000)]
    private static partial Regex AlphaNumericRegex();

    /// <summary>
    /// Removes all non-alphanumeric characters from the input string, leaving only letters and digits.
    /// This method uses a source-generated regular expression for efficient processing.
    /// </summary>
    /// <param name="str">The input string to be filtered.</param>
    /// <returns>A new string containing only alphanumeric characters from the input string.</returns>
    public static string ToAlphaNumeric(
        this string str)
        => AlphaNumericRegex().Replace(str, string.Empty);
}