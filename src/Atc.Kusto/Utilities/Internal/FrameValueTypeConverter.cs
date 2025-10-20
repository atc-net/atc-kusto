namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Provides utility methods for converting Kusto progressive frame values to match DataTable column types.
/// </summary>
/// <remarks>
/// Kusto progressive frames may send values in wire format (e.g., decimals as strings)
/// that need conversion to match the DataTable column types defined by the schema.
/// </remarks>
internal static class FrameValueTypeConverter
{
    /// <summary>
    /// Converts a value from a Kusto progressive frame to match the target column type.
    /// </summary>
    /// <param name="value">The value from the progressive frame.</param>
    /// <param name="targetType">The expected column type from the DataTable schema.</param>
    /// <returns>
    /// The value converted to the target type, or <see cref="DBNull.Value"/> if the value is null.
    /// If conversion fails, returns the original value.
    /// </returns>
    /// <remarks>
    /// This method handles several known type mismatches:
    /// <list type="bullet">
    ///   <item><description>String → SqlDecimal: Kusto sends decimal values as strings in progressive frames.</description></item>
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

        // For other type mismatches, try Convert.ChangeType
        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            // If conversion fails, return original value and let DataTable handle it
            return value;
        }
    }
}