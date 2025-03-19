namespace Atc.Kusto.Serialization.Internal;

/// <summary>
/// Provides a shared Newtonsoft.Json serializer configured to handle Kusto data conversions.
/// This serializer includes the custom <see cref="Atc.Kusto.Serialization.Internal.NewtonsoftObjectConverter"/>.
/// </summary>
public static class KustoJsonSerializerHelper
{
    /// <summary>
    /// A custom JSON serializer configured with a converter that handles the deserialization
    /// of objects from Newtonsoft.Json to System.Text.Json format.
    /// </summary>
    public static readonly Newtonsoft.Json.JsonSerializer Serializer =
        Newtonsoft.Json.JsonSerializer.CreateDefault(new Newtonsoft.Json.JsonSerializerSettings
        {
            Converters = { new NewtonsoftObjectConverter() },
        });
}