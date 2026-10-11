namespace Atc.Kusto.Options;

/// <summary>
/// Options for a paged query.
/// </summary>
public class AtcPagedQueryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="PagedResult{T}.TotalCount"/> is filled with the total
    /// number of items across all pages. Default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// The total is read once, on the first page, from the stored query result's metadata
    /// (<c>.show stored_query_results</c>), which doesn't run the query again. When the first page isn't full, its
    /// item count is the total, so no extra call is made. The total travels in the continuation token, so later
    /// pages need no extra call, and paging stops as soon as the total is reached.
    /// </remarks>
    public bool IncludeTotalCount { get; set; }
}