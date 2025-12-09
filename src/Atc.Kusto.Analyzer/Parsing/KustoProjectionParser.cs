namespace Atc.Kusto.Analyzer.Parsing;

/// <summary>
/// Parser for Kusto project statements to extract projected field names.
/// </summary>
/// <remarks>
/// MA0023 suppressed: The regex patterns intentionally use unnamed capture groups (Groups[1])
/// which are accessed in the code. ExplicitCapture would break this functionality.
/// MA0009 suppressed: The regex patterns are simple and not vulnerable to catastrophic backtracking.
/// They process small strings (Kusto project statements) with bounded input.
/// </remarks>
[SuppressMessage("Performance", "MA0023:Add RegexOptions.ExplicitCapture", Justification = "Unnamed capture groups are intentionally used and accessed via Groups[1]")]
[SuppressMessage("Security", "MA0009:Regular expressions should not be vulnerable to Denial of Service attacks", Justification = "Simple patterns processing bounded input from .kusto files")]
internal static class KustoProjectionParser
{
    /// <summary>
    /// Pattern to find '| project' followed by content.
    /// Captures everything after 'project' until end or next pipe operator.
    /// </summary>
    private static readonly Regex ProjectPattern = new(
        @"\|\s*project\s+(.*?)(?=\s*\||$)",
        RegexOptions.Singleline | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Parses the final project statement from a Kusto file and extracts field names.
    /// </summary>
    /// <param name="kustoFileContent">The content of the .kusto file.</param>
    /// <returns>
    /// A list of projected field names if a final project statement exists;
    /// null if there's no project statement or it's not the final operation.
    /// </returns>
    public static IReadOnlyList<string>? ParseFinalProjection(string kustoFileContent)
    {
        if (string.IsNullOrWhiteSpace(kustoFileContent))
        {
            return null;
        }

        // Remove comments before parsing
        var contentWithoutComments = RemoveComments(kustoFileContent);

        // Find all project matches
        var projectMatches = ProjectPattern.Matches(contentWithoutComments);
        if (projectMatches.Count == 0)
        {
            return null;
        }

        // Get the last project match
        var lastProjectMatch = projectMatches[projectMatches.Count - 1];

        // Check if there's another pipe operation after this project
        // by checking the content after the last project match
        var contentAfterProject = contentWithoutComments.Substring(lastProjectMatch.Index + lastProjectMatch.Length);
        if (HasPipeOperation(contentAfterProject))
        {
            return null;
        }

        // Extract field names from the projection content
        var projectionContent = lastProjectMatch.Groups[1].Value;
        return ExtractFieldNames(projectionContent);
    }

    /// <summary>
    /// Removes single-line and multi-line comments from Kusto content.
    /// </summary>
    private static string RemoveComments(string content)
    {
        // Remove single-line comments (// ...)
        content = Regex.Replace(content, "//.*?$", string.Empty, RegexOptions.Multiline, TimeSpan.FromSeconds(1));

        // Remove multi-line comments (/* ... */)
        content = Regex.Replace(content, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline, TimeSpan.FromSeconds(1));

        return content;
    }

    /// <summary>
    /// Checks if the content contains a pipe operation.
    /// Look for pattern: | followed by a word (operator name)
    /// </summary>
    private static bool HasPipeOperation(string content)
        => Regex.IsMatch(content.Trim(), @"^\s*\|\s*\w+", RegexOptions.Multiline, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Extracts field names from projection content.
    /// </summary>
    private static List<string> ExtractFieldNames(string projectionContent)
    {
        var fieldNames = new List<string>();

        // Split by comma, handling multi-line content
        var segments = SplitByComma(projectionContent);

        foreach (var segment in segments)
        {
            var trimmed = segment.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Extract field name (before = if aliased, or the whole identifier)
            var fieldName = ExtractFieldName(trimmed);
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                fieldNames.Add(fieldName);
            }
        }

        return fieldNames;
    }

    /// <summary>
    /// Splits projection content by commas, handling nested parentheses.
    /// </summary>
    private static List<string> SplitByComma(string content)
    {
        var segments = new List<string>();
        var current = new System.Text.StringBuilder();
        var parenthesesDepth = 0;

        foreach (var ch in content)
        {
            switch (ch)
            {
                case '(':
                    parenthesesDepth++;
                    current.Append(ch);
                    break;
                case ')':
                    parenthesesDepth--;
                    current.Append(ch);
                    break;
                case ',' when parenthesesDepth == 0:
                    segments.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(ch);
                    break;
            }
        }

        // Add the last segment
        if (current.Length > 0)
        {
            segments.Add(current.ToString());
        }

        return segments;
    }

    /// <summary>
    /// Extracts the field name from a projection segment.
    /// Handles both simple fields and aliased fields (Alias = expression).
    /// </summary>
    private static string ExtractFieldName(string segment)
    {
        // Check for alias pattern: Name = expression
        var equalsIndex = segment.IndexOf('=');
        if (equalsIndex > 0)
        {
            // Get the part before '=' and extract the identifier
            var beforeEquals = segment.Substring(0, equalsIndex).Trim();
            return ExtractIdentifier(beforeEquals);
        }

        // Simple field name
        return ExtractIdentifier(segment);
    }

    /// <summary>
    /// Extracts the first valid identifier from a string.
    /// </summary>
    private static string ExtractIdentifier(string text)
    {
        var match = Regex.Match(text.Trim(), @"^(\w+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        return match.Success
            ? match.Groups[1].Value
            : string.Empty;
    }
}