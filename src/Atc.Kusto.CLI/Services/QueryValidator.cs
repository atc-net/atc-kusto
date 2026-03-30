namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Validates KQL query syntax locally using the Kusto Language parser
/// before sending queries to the server.
/// </summary>
public static class QueryValidator
{
    /// <summary>
    /// Validates the given KQL query text for syntax errors.
    /// </summary>
    /// <param name="query">The KQL query text.</param>
    /// <returns>An error message if the query is invalid, or null if valid.</returns>
    public static string? Validate(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "The query text cannot be empty.";
        }

        var parsedQuery = KustoCode.Parse(query);
        var errors = parsedQuery.GetDiagnostics()
            .Where(d => d.Severity == "Error")
            .ToArray();

        if (errors.Length == 0)
        {
            return null;
        }

        var primaryError = errors[0];
        var additionalSuffix = errors.Length > 1
            ? $" (and {errors.Length - 1} more error(s))"
            : string.Empty;
        var locationSuffix = primaryError.HasLocation
            ? FormatLocation(query, primaryError.Start)
            : string.Empty;

        return $"KQL syntax error{locationSuffix}: {primaryError.Message}{additionalSuffix}";
    }

    private static string FormatLocation(
        string query,
        int start)
    {
        var line = 1;
        var column = 1;
        var i = 0;

        while (i < start && i < query.Length)
        {
            if (query[i] == '\r' && i + 1 < query.Length && query[i + 1] == '\n')
            {
                line++;
                column = 1;
                i += 2;
            }
            else if (query[i] is '\r' or '\n')
            {
                line++;
                column = 1;
                i++;
            }
            else
            {
                column++;
                i++;
            }
        }

        return $" at line {line}, column {column}";
    }
}