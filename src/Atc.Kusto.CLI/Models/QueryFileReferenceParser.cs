namespace Atc.Kusto.CLI.Models;

/// <summary>
/// Parses file references in the format "path:start-end" into
/// <see cref="QueryFileReference"/> values, handling Windows drive letters.
/// </summary>
public static partial class QueryFileReferenceParser
{
    /// <summary>
    /// Parses a file reference string that may contain a line range suffix.
    /// </summary>
    /// <param name="fileReference">The file reference string (e.g. "query.kql:5-10").</param>
    /// <returns>A parsed file reference with optional line range.</returns>
    /// <exception cref="ArgumentException">Thrown when the line range syntax is invalid.</exception>
    public static QueryFileReference Parse(string fileReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileReference);

        var trimmed = fileReference.Trim();
        var colonIndex = trimmed.LastIndexOf(':');

        if (colonIndex < 0 || IsWindowsDriveSeparator(trimmed, colonIndex))
        {
            return new QueryFileReference(trimmed, null);
        }

        var filePath = trimmed[..colonIndex];
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Query file path cannot be empty.",
                nameof(fileReference));
        }

        var rangeText = trimmed[(colonIndex + 1)..];
        var match = LineRangePattern().Match(rangeText);
        if (!match.Success)
        {
            throw new ArgumentException(
                $"Query file range '{rangeText}' is invalid. Use '<path>:<start>-<end>'.",
                nameof(fileReference));
        }

        if (!int.TryParse(match.Groups["start"].Value, out var startLine) ||
            !int.TryParse(match.Groups["end"].Value, out var endLine) ||
            startLine <= 0 ||
            endLine <= 0)
        {
            throw new ArgumentException(
                "Query file line numbers must be positive integers.",
                nameof(fileReference));
        }

        if (endLine < startLine)
        {
            throw new ArgumentException(
                $"Query file range '{rangeText}' is invalid. The end line must be greater than or equal to the start line.",
                nameof(fileReference));
        }

        return new QueryFileReference(filePath, new QueryLineRange(startLine, endLine));
    }

    private static bool IsWindowsDriveSeparator(
        string reference,
        int colonIndex)
        => colonIndex == 1 &&
           char.IsAsciiLetter(reference[0]) &&
           (OperatingSystem.IsWindows() ||
            (reference.Length > 2 &&
             (reference[2] == '\\' || reference[2] == '/')));

    [GeneratedRegex(
        @"^(?<start>-?\d+)-(?<end>-?\d+)$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex LineRangePattern();
}