namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Provides shared JSON serializer options for consistent Kusto data deserialization across the library.
/// </summary>
internal static class KustoJsonSerializerOptions
{
    /// <summary>
    /// Gets the default JSON serializer options configured for Kusto data deserialization.
    /// Includes enum string conversion, case-insensitive property matching, and support for reading numbers from strings.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}