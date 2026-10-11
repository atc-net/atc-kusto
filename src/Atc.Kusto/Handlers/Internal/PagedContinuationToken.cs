namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// The continuation token of a paged query: <c>v2;{nonce};{itemsReturned}</c>, followed by <c>;{totalCount}</c>
/// when the total count was requested.
/// </summary>
/// <remarks>
/// The token never contains the queryId: the handler calculates the queryId from the query
/// (see <see cref="IQueryIdProvider"/>), so a client can't choose which stored result it reads.
/// </remarks>
/// <param name="Nonce">The random value of the paging run, or <see langword="null"/> when a session id is used.</param>
/// <param name="ItemsReturned">The number of items returned so far.</param>
/// <param name="TotalCount">The total number of items, or <see langword="null"/> when it wasn't requested.</param>
internal sealed record PagedContinuationToken(
    string? Nonce,
    long ItemsReturned,
    long? TotalCount = null)
{
    private const string Version = "v2";
    private const int MaxNonceLength = 64;

    /// <summary>
    /// Parses a continuation token.
    /// </summary>
    /// <param name="token">The token from the client.</param>
    /// <returns>
    /// The token, or <see langword="null"/> when it isn't a valid v2 token (also for tokens from earlier versions).
    /// </returns>
    public static PagedContinuationToken? Parse(string token)
    {
        var parts = token.Split(';');
        if (parts.Length is not (3 or 4) ||
            !string.Equals(parts[0], Version, StringComparison.Ordinal))
        {
            return null;
        }

        var nonce = parts[1];
        if (nonce.Length > MaxNonceLength ||
            !string.Equals(nonce.ToAlphanumeric(), nonce, StringComparison.Ordinal))
        {
            return null;
        }

        // NumberStyles.None: digits only, so no sign, spaces, decimals or thousands separators.
        if (!long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var itemsReturned))
        {
            return null;
        }

        long? totalCount = null;
        if (parts.Length == 4)
        {
            if (!long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var total))
            {
                return null;
            }

            totalCount = total;
        }

        return new PagedContinuationToken(
            nonce.Length > 0 ? nonce : null,
            itemsReturned,
            totalCount);
    }

    /// <summary>
    /// Creates the token for the next page, or returns <see langword="null"/> when this was the last page.
    /// </summary>
    /// <param name="nonce">The random value of the paging run, or <see langword="null"/>.</param>
    /// <param name="itemsReturned">The number of items returned so far, including this page.</param>
    /// <param name="pageItemCount">The number of items on this page.</param>
    /// <param name="pageSize">The number of items in a full page.</param>
    /// <param name="totalCount">The total number of items, or <see langword="null"/> when it wasn't requested.</param>
    /// <returns>The token for the next page, or <see langword="null"/>.</returns>
    public static PagedContinuationToken? CreateNext(
        string? nonce,
        long itemsReturned,
        int pageItemCount,
        int pageSize,
        long? totalCount)
    {
        // A page that isn't full is the last one, and so is the page that reaches the total.
        var isLastPage = pageItemCount < pageSize ||
                         (totalCount is not null && itemsReturned >= totalCount);

        return isLastPage
            ? null
            : new PagedContinuationToken(nonce, itemsReturned, totalCount);
    }

    /// <summary>
    /// Writes the token as <c>v2;{nonce};{itemsReturned}</c>, followed by <c>;{totalCount}</c> when it is known.
    /// </summary>
    /// <returns>The token.</returns>
    public override string ToString()
        => TotalCount is null
            ? string.Create(CultureInfo.InvariantCulture, $"{Version};{Nonce};{ItemsReturned}")
            : string.Create(CultureInfo.InvariantCulture, $"{Version};{Nonce};{ItemsReturned};{TotalCount}");
}