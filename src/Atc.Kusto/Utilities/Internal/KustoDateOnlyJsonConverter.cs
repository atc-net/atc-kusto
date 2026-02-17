namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Custom JSON converter for <see cref="DateOnly"/> values that handles Kusto's behavior of returning date values as full datetime strings.
/// </summary>
/// <remarks>
/// Kusto has no native date-only type — functions like <c>startofday()</c> and <c>bin(Timestamp, 1d)</c> still return
/// <c>datetime</c> values (e.g. <c>2024-01-15T00:00:00Z</c>). This converter allows C# result types to use
/// <see cref="DateOnly"/> properties that correctly deserialize from both:
/// <list type="bullet">
///   <item><description>Date-only strings (e.g. <c>"2024-01-15"</c>)</description></item>
///   <item><description>Full datetime strings (e.g. <c>"2024-01-15T00:00:00Z"</c>)</description></item>
/// </list>
/// </remarks>
public sealed class KustoDateOnlyJsonConverter : JsonConverter<DateOnly>
{
    /// <summary>
    /// Reads and converts a JSON string to a <see cref="DateOnly"/> value.
    /// </summary>
    /// <param name="reader">The reader to read JSON from.</param>
    /// <param name="typeToConvert">The type to convert (will be <see cref="DateOnly"/>).</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The <see cref="DateOnly"/> value represented by the JSON string.</returns>
    /// <exception cref="JsonException">Thrown when the string value is null or cannot be parsed as a date.</exception>
    /// <remarks>
    /// Handles two cases:
    /// <list type="bullet">
    ///   <item><description>Date-only strings (e.g. <c>"2024-01-15"</c>) — parsed directly via <see cref="DateOnly.TryParse(string?, IFormatProvider?, out DateOnly)"/></description></item>
    ///   <item><description>Full datetime strings (e.g. <c>"2024-01-15T00:00:00Z"</c>) — parsed as <see cref="DateTimeOffset"/> and converted to <see cref="DateOnly"/></description></item>
    /// </list>
    /// </remarks>
    public override DateOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString()
            ?? throw new JsonException(
                $"Unable to convert null to {nameof(DateOnly)}.");

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var dateOnly))
        {
            return dateOnly;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out var dateTimeOffset))
        {
            return DateOnly.FromDateTime(dateTimeOffset.DateTime);
        }

        throw new JsonException(
            $"Unable to convert \"{value}\" to {nameof(DateOnly)}.");
    }

    /// <summary>
    /// Writes a <see cref="DateOnly"/> value as an ISO 8601 date string.
    /// </summary>
    /// <param name="writer">The writer to write JSON to.</param>
    /// <param name="value">The <see cref="DateOnly"/> value to write.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <remarks>
    /// Always writes the value as a date-only ISO 8601 string (e.g. <c>"2024-01-15"</c>).
    /// </remarks>
    public override void Write(
        Utf8JsonWriter writer,
        DateOnly value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
    }
}