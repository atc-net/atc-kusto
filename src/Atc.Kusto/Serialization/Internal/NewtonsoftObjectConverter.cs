namespace Atc.Kusto.Serialization.Internal;

/// <summary>
/// A custom JSON converter that facilitates the conversion of objects between
/// Newtonsoft.Json and System.Text.Json formats.
/// </summary>
internal sealed class NewtonsoftObjectConverter : Newtonsoft.Json.JsonConverter
{
    /// <summary>
    /// Determines whether this converter can handle the conversion of the specified object type.
    /// This converter is designed to handle objects of type <see cref="object"/>.
    /// </summary>
    /// <param name="objectType">The type of object to check for conversion compatibility.</param>
    /// <returns>True if the object type is <see cref="object"/>, otherwise false.</returns>
    public override bool CanConvert(
        Type objectType)
        => objectType == typeof(object);

    /// <summary>
    /// Reads JSON data and converts it from Newtonsoft.Json's representation to System.Text.Json's representation.
    /// This method is essential for bridging the gap between the two JSON libraries.
    /// </summary>
    /// <param name="reader">The <see cref="Newtonsoft.Json.JsonReader"/> to read the JSON data from.</param>
    /// <param name="objectType">The type of object to deserialize to.</param>
    /// <param name="existingValue">The existing value of the object being read (usually null).</param>
    /// <param name="serializer">The <see cref="Newtonsoft.Json.JsonSerializer"/> that called this method.</param>
    /// <returns>The deserialized object as a System.Text.Json.JsonElement, or null if the conversion could not be performed.</returns>
    public override object? ReadJson(
        Newtonsoft.Json.JsonReader reader,
        Type objectType,
        object? existingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        => reader switch
        {
            Newtonsoft.Json.Linq.JTokenReader { CurrentToken: { } token } => Convert(token),
            _ => null,
        };

    public override void WriteJson(
        Newtonsoft.Json.JsonWriter writer,
        object? value,
        Newtonsoft.Json.JsonSerializer serializer)
        => throw new NotSupportedException();

    /// <summary>
    /// Converts a <see cref="Newtonsoft.Json.Linq.JToken"/> to a <see cref="System.Text.Json.JsonElement"/>,
    /// facilitating the interoperability between Newtonsoft.Json and System.Text.Json.
    /// </summary>
    /// <param name="token">The <see cref="Newtonsoft.Json.Linq.JToken"/> to convert.</param>
    /// <returns>A <see cref="System.Text.Json.JsonElement"/> representing the converted data.</returns>
    private static System.Text.Json.JsonElement Convert(
        Newtonsoft.Json.Linq.JToken token)
    {
        var utf8 = Encoding.UTF8.GetBytes(token.ToString());
        var jsonReader = new System.Text.Json.Utf8JsonReader(utf8);

        return System.Text.Json.JsonElement.ParseValue(ref jsonReader);
    }
}