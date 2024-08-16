namespace Atc.Kusto;

/// <summary>
/// Represents a paginated result set for Kusto queries, containing a list of items
/// and an optional continuation token for retrieving subsequent pages of data.
/// </summary>
/// <typeparam name="T">The type of items contained in the result set.</typeparam>
/// <param name="Items">The list of items returned in the current page.</param>
/// <param name="ContinuationToken">
/// An optional token that can be used to fetch the next page of results.
/// If null, it indicates that there are no more pages to retrieve.
/// </param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    string? ContinuationToken);