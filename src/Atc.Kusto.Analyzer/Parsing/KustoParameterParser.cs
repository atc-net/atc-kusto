namespace Atc.Kusto.Analyzer.Parsing;

/// <summary>
/// Parser for Kusto query parameter declarations.
/// </summary>
/// <remarks>
/// MA0023 suppressed: The regex patterns intentionally use unnamed capture groups (Groups[1], Groups[2])
/// which are accessed in the code. ExplicitCapture would break this functionality.
/// MA0009 suppressed: The regex patterns are simple and not vulnerable to catastrophic backtracking.
/// They process small strings (Kusto parameter declarations) with bounded input.
/// </remarks>
[SuppressMessage("Performance", "MA0023:Add RegexOptions.ExplicitCapture", Justification = "Unnamed capture groups are intentionally used and accessed via Groups[1] and Groups[2]")]
[SuppressMessage("Security", "MA0009:Regular expressions should not be vulnerable to Denial of Service attacks", Justification = "Simple patterns processing bounded input from .kusto files")]
internal static class KustoParameterParser
{
    private const string DeclareQueryParametersPattern = @"declare\s+query_parameters\s*\((.*?)\)\s*;";
    private const string ParameterPattern = @"(\w+)\s*:\s*(\w+)(?:\s*=\s*[^,)]+)?";

    /// <summary>
    /// Parses Kusto parameter declarations from a .kusto file content.
    /// </summary>
    /// <param name="kustoFileContent">The content of the .kusto file.</param>
    /// <returns>A list of parsed parameters in the order they appear.</returns>
    public static IReadOnlyList<KustoParameter> ParseParameters(string kustoFileContent)
    {
        if (string.IsNullOrWhiteSpace(kustoFileContent))
        {
            return [];
        }

        // Find the declare query_parameters block
        var declareMatch = Regex.Match(
            kustoFileContent,
            DeclareQueryParametersPattern,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (!declareMatch.Success)
        {
            // No parameter declaration found
            return [];
        }

        var parametersBlock = declareMatch.Groups[1].Value;
        var parameters = new List<KustoParameter>();

        // Parse individual parameters
        var paramMatches = Regex.Matches(
            parametersBlock,
            ParameterPattern,
            RegexOptions.Singleline);

        foreach (Match match in paramMatches)
        {
            var name = match.Groups[1].Value.Trim();
            var type = match.Groups[2].Value.Trim();

            // Check if there's a default value
            var fullMatch = match.Value;
            var hasDefaultValue = fullMatch.Contains('=');

            parameters.Add(new KustoParameter(name, type, hasDefaultValue));
        }

        return parameters;
    }
}