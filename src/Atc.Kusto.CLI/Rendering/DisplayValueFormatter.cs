namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Formats cell values for human-readable display: thousand separators for numbers,
/// smart datetime formatting, and preservation of zero-padded identifiers.
/// </summary>
internal static class DisplayValueFormatter
{
    public static string FormatCellValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        // Skip zero-padded values that are likely identifiers (e.g., "001234")
        var numericStart = value[0] == '-' ? 1 : 0;
        if (value.Length > numericStart + 1 &&
            value[numericStart] == '0' &&
            char.IsDigit(value[numericStart + 1]))
        {
            return value;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue.ToString("N0", CultureInfo.InvariantCulture);
        }

        if (decimal.TryParse(
                value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var decimalValue))
        {
            var dotIndex = value.IndexOf('.', StringComparison.Ordinal);
            if (dotIndex >= 0)
            {
                var decimalPlaces = value.Length - dotIndex - 1;
                return decimalValue.ToString($"N{decimalPlaces}", CultureInfo.InvariantCulture);
            }

            return decimalValue.ToString("N0", CultureInfo.InvariantCulture);
        }

        // Format ISO 8601 datetime strings (YYYY-MM-DDTHH:mm:ss...)
        if (value.Length >= 20 &&
            char.IsDigit(value[0]) &&
            value[4] == '-' &&
            value[7] == '-' &&
            value[10] == 'T' &&
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
        {
            if (dateTimeValue.TimeOfDay == TimeSpan.Zero)
            {
                return dateTimeValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            // Non-midnight: replace T with space for readability, preserving timezone
            return string.Concat(value.AsSpan(0, 10), " ", value.AsSpan(11));
        }

        return value;
    }

    public static bool IsNumericValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
               decimal.TryParse(
                   value,
                   NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out _);
    }
}