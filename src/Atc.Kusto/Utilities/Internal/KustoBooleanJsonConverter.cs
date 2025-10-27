namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Custom JSON converter for boolean values that handles Kusto's behavior of returning boolean expressions as numeric types.
/// </summary>
/// <remarks>
/// Kusto's tobool(...) function and boolean expressions return sbyte (0 or 1) to .NET instead of true boolean values.
/// This converter ensures that boolean DTO properties can correctly deserialize from both:
/// - Standard JSON boolean tokens (true/false)
/// - Numeric tokens where 0 = false and any non-zero value = true
/// This is necessary because the Kusto SDK returns numeric values for boolean columns in query results.
/// </remarks>
public sealed class KustoBooleanJsonConverter : JsonConverter<bool>
{
    /// <summary>
    /// Reads and converts JSON to a boolean value.
    /// </summary>
    /// <param name="reader">The reader to read JSON from.</param>
    /// <param name="typeToConvert">The type to convert (will be bool).</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The boolean value represented by the JSON token.</returns>
    /// <exception cref="JsonException">Thrown when the token type cannot be converted to boolean.</exception>
    /// <remarks>
    /// Handles three cases:
    /// - JsonTokenType.True: Returns true
    /// - JsonTokenType.False: Returns false
    /// - JsonTokenType.Number: Returns true if non-zero, false if zero (Kusto behavior)
    /// </remarks>
    public override bool Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.GetInt32() != 0,
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to boolean"),
        };

    /// <summary>
    /// Writes a boolean value as JSON.
    /// </summary>
    /// <param name="writer">The writer to write JSON to.</param>
    /// <param name="value">The boolean value to write.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <remarks>
    /// Always writes standard JSON boolean values (true/false), not numeric representations.
    /// </remarks>
    public override void Write(
        Utf8JsonWriter writer,
        bool value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteBooleanValue(value);
    }
}