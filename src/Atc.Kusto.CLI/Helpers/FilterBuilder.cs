namespace Atc.Kusto.CLI.Helpers;

/// <summary>
/// Builds KQL where-clause fragments from smart filter expressions.
/// Supports contains (plain text), startswith (^prefix), endswith (suffix$),
/// and exact match (^exact$).
/// </summary>
public static class FilterBuilder
{
    /// <summary>
    /// Builds a KQL filter expression for the given column and filter value.
    /// </summary>
    /// <param name="columnName">The KQL column name to filter on.</param>
    /// <param name="filter">The filter expression.</param>
    /// <returns>A KQL where-clause fragment, e.g. "| where TableName contains 'Storm'".</returns>
    public static string Build(string columnName, string filter)
    {
        ArgumentNullException.ThrowIfNull(columnName);
        ArgumentNullException.ThrowIfNull(filter);

        var hasPrefix = filter.StartsWith('^');
        var hasSuffix = filter.EndsWith('$');

        if (hasPrefix && hasSuffix)
        {
            var value = EscapeKqlString(filter[1..^1]);
            return $"| where {columnName} startswith '{value}' and {columnName} endswith '{value}'";
        }

        if (hasPrefix)
        {
            var value = EscapeKqlString(filter[1..]);
            return $"| where {columnName} startswith '{value}'";
        }

        if (hasSuffix)
        {
            var value = EscapeKqlString(filter[..^1]);
            return $"| where {columnName} endswith '{value}'";
        }

        var containsValue = EscapeKqlString(filter);
        return $"| where {columnName} contains '{containsValue}'";
    }

    private static string EscapeKqlString(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);
}