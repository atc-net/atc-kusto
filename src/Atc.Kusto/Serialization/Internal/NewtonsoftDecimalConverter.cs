namespace Atc.Kusto.Serialization.Internal;

/// <summary>
/// A custom JSON converter that supports reading decimal and nullable decimal values
/// from <see cref="Newtonsoft.Json"/> when deserializing Kusto responses.
/// <para>
/// Azure Data Explorer (ADX) may return decimal values wrapped in a JSON structure
/// (e.g. originating from <see cref="System.Data.SqlTypes.SqlDecimal"/>). This converter extracts
/// the underlying numeric value by reading the <c>Value</c> property from the token and converting
/// it to a <see cref="decimal"/>. Write support is intentionally not implemented because the
/// library only performs deserialization of Kusto query results.
/// </para>
/// </summary>
public sealed class NewtonsoftDecimalConverter : Newtonsoft.Json.JsonConverter
{
    /// <summary>
    /// Determines whether this converter can handle the specified <paramref name="objectType"/>.
    /// This converter is applicable for <see cref="decimal"/> and <see cref="Nullable{T}"/> of <see cref="decimal"/>.
    /// </summary>
    /// <param name="objectType">The target type to evaluate.</param>
    /// <returns><see langword="true"/> if the type is <see cref="decimal"/> or a nullable decimal; otherwise <see langword="false"/>.</returns>
    public override bool CanConvert(Type objectType)
        => objectType == typeof(decimal) ||
           objectType == typeof(decimal?);

    /// <summary>
    /// Reads the JSON representation of a decimal value and converts it to a CLR <see cref="decimal"/>.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the value to convert.</param>
    /// <param name="objectType">The type of the object to create (expected to be <see cref="decimal"/> or a nullable decimal).</param>
    /// <param name="existingValue">The existing value of the object being read (unused).</param>
    /// <param name="serializer">The serializer invoking this method (unused).</param>
    /// <returns>
    /// The extracted <see cref="decimal"/> value when the token is recognized; otherwise <see langword="null"/>.
    /// </returns>
    public override object? ReadJson(
        Newtonsoft.Json.JsonReader reader,
        Type objectType,
        object? existingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        => reader switch
        {
            // Extract the underlying decimal numeric value from the JToken's Value property
            Newtonsoft.Json.Linq.JTokenReader { CurrentToken: { } token }
                => token.Value<decimal>(nameof(SqlDecimal.Value)),
            _ => null,
        };

    /// <summary>
    /// Writing JSON is not supported for this converter as it is only intended for deserialization.
    /// </summary>
    /// <param name="writer">The JSON writer (unused).</param>
    /// <param name="value">The value to write (unused).</param>
    /// <param name="serializer">The serializer invoking this method (unused).</param>
    /// <exception cref="NotSupportedException">Always thrown since writing is not implemented.</exception>
    public override void WriteJson(
        Newtonsoft.Json.JsonWriter writer,
        object? value,
        Newtonsoft.Json.JsonSerializer serializer)
        => throw new NotSupportedException();
}