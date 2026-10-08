namespace Atc.Kusto.Utilities.Internal;

/// <summary>
/// Provides shared JSON serializer options for consistent Kusto data deserialization across the library.
/// </summary>
internal static class KustoJsonSerializerOptions
{
    /// <summary>
    /// Gets the default JSON serializer options used for reading query results and writing ingestion payloads.
    /// Includes enum string conversion, the Kusto boolean and <see cref="DateOnly"/> converters, case-insensitive
    /// property matching, support for reading numbers from strings, and camelCase property names.
    /// </summary>
    /// <remarks>
    /// The camelCase naming policy only changes what is <em>written</em> (ingestion JSON, e.g. <c>{"serialNumber":…}</c>),
    /// so that it matches the camelCase column names and <c>$.camelCase</c> ingestion mapping paths typical of Kusto
    /// tables fed by Event Hubs. Reading is unaffected because property matching is case-insensitive.
    /// </remarks>
    public static JsonSerializerOptions Default { get; } = new()
    {
        Converters = { new JsonStringEnumConverter(), new KustoBooleanJsonConverter(), new KustoDateOnlyJsonConverter() },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}