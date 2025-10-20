// ReSharper disable CheckNamespace
namespace Atc.Kusto;

/// <summary>
/// Provides extension methods for converting <see cref="SqlDecimal"/> values to .NET <see cref="decimal"/> type.
/// These methods handle the precision and scale constraints when converting from Kusto's 128-bit decimals
/// to .NET's 96-bit decimal type.
/// </summary>
public static class SqlDecimalExtensions
{
    private const int DotNetDecimalMaxPrecision = 28;
    private const int DotNetDecimalMaxScale = 27;

    /// <summary>
    /// Converts a <see cref="SqlDecimal"/> to a <see cref="decimal"/> by safely adjusting precision and scale
    /// to fit within .NET decimal's constraints (28 max precision, 27 max scale).
    /// <para>
    /// Azure Data Explorer (Kusto) uses 128-bit decimals that can exceed .NET's 96-bit decimal capacity.
    /// This method ensures safe conversion by:
    /// <list type="bullet">
    /// <item><description>Validating the integer part fits within .NET decimal's precision</description></item>
    /// <item><description>Adjusting scale if necessary to fit the total precision constraint</description></item>
    /// <item><description>Using <see cref="SqlDecimal.ConvertToPrecScale"/> for safe rounding when needed</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="sqlDecimal">The SQL decimal value to convert.</param>
    /// <returns>A <see cref="decimal"/> value representing the SQL decimal, adjusted if necessary to fit .NET constraints.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the integer part (digits before the decimal point) exceeds .NET decimal's maximum precision capacity of 28 digits.
    /// This indicates the value is too large to represent as a .NET decimal.
    /// </exception>
    /// <example>
    /// <code>
    /// // Simple conversion
    /// var sqlDec = new SqlDecimal(123.45m);
    /// decimal result = sqlDec.ToDecimal(); // 123.45
    ///
    /// // High precision conversion (automatically adjusted)
    /// var highPrecision = new SqlDecimal(38, 20, true, ...); // 38 digits, 20 scale
    /// decimal adjusted = highPrecision.ToDecimal(); // Adjusted to fit 28 precision
    /// </code>
    /// </example>
    public static decimal ToDecimal(this SqlDecimal sqlDecimal)
    {
        var integerDigits = sqlDecimal.Precision - sqlDecimal.Scale;
        if (integerDigits > DotNetDecimalMaxPrecision)
        {
            throw new OverflowException(
                $"Integer part ({integerDigits} digits) exceeds .NET decimal capacity of {DotNetDecimalMaxPrecision} digits. " +
                $"SqlDecimal value has precision={sqlDecimal.Precision}, scale={sqlDecimal.Scale}.");
        }

        var maxAvailableScale = DotNetDecimalMaxPrecision - integerDigits;

        // Calculate target scale as the minimum of all constraints:
        // 1. The actual scale from SqlDecimal
        // 2. .NET decimal's maximum scale (27)
        // 3. Available scale given the integer part size
        var scaleConstrainedByDotNet = System.Math.Min((int)sqlDecimal.Scale, DotNetDecimalMaxScale);
        var targetScale = System.Math.Min(scaleConstrainedByDotNet, maxAvailableScale);

        var targetPrecision = System.Math.Min(sqlDecimal.Precision, integerDigits + targetScale);

        if (targetPrecision != sqlDecimal.Precision || targetScale != sqlDecimal.Scale)
        {
            // Precision or scale needs adjustment - use safe conversion
            var safeDecimal = SqlDecimal.ConvertToPrecScale(sqlDecimal, targetPrecision, targetScale);
            return (decimal)safeDecimal;
        }

        // Direct conversion is safe
        return (decimal)sqlDecimal;
    }
}