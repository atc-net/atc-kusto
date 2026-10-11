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
    string? ContinuationToken)
{
    /// <summary>
    /// Gets the total number of items across all pages, or <see langword="null"/> when it wasn't requested with
    /// <see cref="Options.AtcPagedQueryOptions.IncludeTotalCount"/>.
    /// </summary>
    /// <remarks>
    /// When the stored query result had expired and the query ran again, this is the total of the new run.
    /// </remarks>
    public long? TotalCount { get; init; }
}