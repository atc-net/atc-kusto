namespace Atc.Kusto.Serialization.Internal;

/// <summary>
/// Provides a shared Newtonsoft.Json serializer configured to handle Kusto data conversions.
/// This serializer includes the custom <see cref="Atc.Kusto.Serialization.Internal.NewtonsoftObjectConverter"/> for
/// bridging generic object payloads to <see cref="System.Text.Json"/> as well as the
/// <see cref="Atc.Kusto.Serialization.Internal.NewtonsoftDecimalConverter"/> for extracting decimal values
/// returned in structured tokens.
/// </summary>
public static class KustoJsonSerializerHelper
{
    /// <summary>
    /// A custom JSON serializer configured with converters that handle the deserialization
    /// of generic objects and decimal numeric values from Newtonsoft.Json to System.Text.Json representations.
    /// </summary>
    public static readonly Newtonsoft.Json.JsonSerializer Serializer =
        Newtonsoft.Json.JsonSerializer.CreateDefault(new Newtonsoft.Json.JsonSerializerSettings
        {
            Converters = { new NewtonsoftObjectConverter(), new NewtonsoftDecimalConverter() },
        });
}