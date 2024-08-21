// ReSharper disable CheckNamespace
namespace Atc.Kusto;

/// <summary>
/// Provides extension methods for handling Kusto scripts and
/// simplifying interaction with Kusto by preparing query parameters and request properties.
/// </summary>
public static class KustoScriptExtensions
{
    /// <summary>
    /// A JSON serializer configured to convert enums to their string representations,
    /// ensuring compatibility when passing dynamic objects to Kusto queries.
    /// </summary>
    private static readonly Newtonsoft.Json.JsonSerializer Serializer = new()
    {
        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() },
    };

    /// <summary>
    /// Generates a <see cref="ClientRequestProperties"/> object for a given Kusto script,
    /// including formatted parameters and a unique client request ID for tracing and logging.
    /// This simplifies the process of setting up the necessary properties for a Kusto query.
    /// </summary>
    /// <param name="script">The Kusto script instance.</param>
    /// <returns>A configured <see cref="ClientRequestProperties"/> object.</returns>
    public static ClientRequestProperties GetClientRequestProperties(
        this IKustoScript script)
        => new(options: null, script.GetCslParameters())
        {
            ClientRequestId = Guid.NewGuid().ToString(),
        };

    /// <summary>
    /// Retrieves the script's parameters and converts them into a collection of key-value pairs
    /// formatted as Kusto-compatible strings, making it easier to pass parameters to Kusto queries.
    /// </summary>
    /// <param name="script">The Kusto script instance.</param>
    /// <returns>A collection of key-value pairs representing the script's parameters.</returns>
    public static IEnumerable<KeyValuePair<string, string>> GetCslParameters(
        this IKustoScript script)
    {
        ArgumentNullException.ThrowIfNull(script);

        return script
            .GetParameters()
            .Select(p => KeyValuePair.Create(
                p.Key,
                GetCslValue(p.Value)));
    }

    /// <summary>
    /// Converts an object to its corresponding Kusto Query Language (CSL) string representation,
    /// handling various data types to ensure they are correctly formatted for Kusto queries.
    /// This method abstracts the complexities of Kusto's type system.
    /// </summary>
    /// <param name="value">The value to be converted to CSL string representation.</param>
    /// <returns>A string representing the value in CSL format.</returns>
    public static string GetCslValue(
        object value)
        => value switch
        {
            bool b => CslBoolLiteral.AsCslString(b),
            int i => CslIntLiteral.AsCslString(i),
            long i => CslLongLiteral.AsCslString(i),
            decimal d => CslDecimalLiteral.AsCslString(d),
            double d => CslRealLiteral.AsCslString(d),
            TimeSpan t => CslTimeSpanLiteral.AsCslString(t),
            DateTime d => CslDateTimeLiteral.AsCslString(d),
            DateTimeOffset d => CslDateTimeLiteral.AsCslString(d.UtcDateTime),
            string s => CslStringLiteral.AsCslString(s),
            Enum e => CslStringLiteral.AsCslString(Enum.GetName(e.GetType(), e)),
            { } o => CslDynamicLiteral.AsCslString(Newtonsoft.Json.Linq.JToken.FromObject(o, Serializer)),
        };
}