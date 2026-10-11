namespace Atc.Kusto.Providers.Internal;

/// <summary>
/// Calculates the queryId of a paged query: the name its result is stored under in Kusto
/// (<c>stored_query_result('{queryId}')</c>).
/// </summary>
internal interface IQueryIdProvider
{
    /// <summary>
    /// Calculates the queryId (the stored query result name) for <paramref name="query"/>.
    /// </summary>
    /// <remarks>
    /// The queryId is the query type's name followed by a hash of the query type, <paramref name="sessionId"/>,
    /// <paramref name="nonce"/> and a fingerprint of the query (its text and parameters). So every instance calculates
    /// the same queryId, and a continuation token can only continue the exact query it was created for: the same query
    /// with other parameters (e.g. another partner) gets another queryId.
    /// </remarks>
    /// <param name="query">The paged query.</param>
    /// <param name="sessionId">The caller's session id, or <see langword="null"/>.</param>
    /// <param name="nonce">The random value of this paging run (used when there is no session), or <see langword="null"/>.</param>
    /// <returns>An alphanumeric queryId.</returns>
    string Create(
        IKustoScript query,
        string? sessionId,
        string? nonce);
}