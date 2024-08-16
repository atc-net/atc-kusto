namespace Atc.Kusto.Providers.Internal;

/// <summary>
/// Provides functionality to generate a unique query identifier based on the query type and an optional session ID.
/// This identifier is alphanumeric, making it suitable for use in scenarios where only alphanumeric characters are allowed.
/// </summary>
internal interface IQueryIdProvider
{
    /// <summary>
    /// Creates a unique identifier for a query based on the query type name and an optional session ID.
    /// If the session ID is provided, it is appended to the query type name. If no session ID is provided,
    /// a new GUID is generated, formatted as a string without hyphens, and appended to the query type name.
    /// The resulting identifier is then filtered to contain only alphanumeric characters.
    /// </summary>
    /// <param name="queryType">The <see cref="Type"/> of the query for which the identifier is being generated.</param>
    /// <param name="sessionId">An optional session ID to be included in the identifier. If null, a new GUID is used instead.</param>
    /// <returns>A string representing the unique alphanumeric query identifier.</returns>
    string Create(
        Type queryType,
        string? sessionId);
}