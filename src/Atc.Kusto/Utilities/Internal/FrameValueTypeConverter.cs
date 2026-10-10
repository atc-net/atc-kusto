namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Provides utility methods for converting Kusto progressive frame values to match the column types of the table schema.
/// </summary>
/// <remarks>
/// Kusto progressive frames send values in wire format (as strings, apart from <c>dynamic</c> values)
/// that need conversion to match the column types defined by the schema.
/// </remarks>
internal static class FrameValueTypeConverter
{
    /// <summary>
    /// Converts a value from a Kusto progressive frame to match the target column type.
    /// </summary>
    /// <param name="value">The value from the progressive frame.</param>
    /// <param name="targetType">The expected column type from the table schema.</param>
    /// <returns>
    /// The value converted to the target type, or <see cref="DBNull.Value"/> if the value is null.
    /// If conversion fails, returns the original value.
    /// </returns>
    /// <remarks>
    /// This method handles several known type mismatches:
    /// <list type="bullet">
    ///   <item><description>String → SqlDecimal: Kusto sends decimal values as strings in progressive frames.</description></item>
    ///   <item><description>String → double: the SDK writes real values with the current culture (e.g. "2,5" on da-DK).</description></item>
    ///   <item><description>String → Guid, TimeSpan and DateTime (as UTC), which <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> can't do or would turn into local time.</description></item>
    ///   <item><description>Other mismatches: Uses <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> with invariant culture.</description></item>
    /// </list>
    /// </remarks>
    public static object? ConvertToColumnType(
        object? value,
        Type targetType)
    {
        if (value is null or DBNull)
        {
            return DBNull.Value;
        }

        // If types already match, return as-is
        if (value.GetType() == targetType)
        {
            return value;
        }

        // Handle string → SqlDecimal conversion (Kusto sends decimals as strings in progressive frames)
        if (value is string stringValue &&
            targetType == typeof(SqlDecimal) &&
            decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return new SqlDecimal(decimalValue);
        }

        // Handle string → double: the SDK writes real values with the current culture ("2,5" on da-DK),
        // so read them back with the same culture. The invariant culture would read "2,5" as 25.
        if (value is string realText &&
            targetType == typeof(double) &&
            double.TryParse(realText, NumberStyles.Float, CultureInfo.CurrentCulture, out var realValue))
        {
            return realValue;
        }

        if (value is string guidText &&
            targetType == typeof(Guid) &&
            Guid.TryParse(guidText, out var guidValue))
        {
            return guidValue;
        }

        if (value is string timeSpanText &&
            targetType == typeof(TimeSpan) &&
            TimeSpan.TryParse(timeSpanText, CultureInfo.InvariantCulture, out var timeSpanValue))
        {
            return timeSpanValue;
        }

        // Kusto datetimes are UTC ("2024-01-02T03:04:05.678Z"); keep them UTC instead of converting to local time.
        if (value is string dateTimeText &&
            targetType == typeof(DateTime) &&
            DateTime.TryParse(dateTimeText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dateTimeValue))
        {
            return dateTimeValue;
        }

        // For other type mismatches, try Convert.ChangeType
        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            // If conversion fails, return the original value
            return value;
        }
    }
}