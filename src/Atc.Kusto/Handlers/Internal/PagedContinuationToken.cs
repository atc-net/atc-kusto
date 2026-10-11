namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// The continuation token of a paged query: <c>v2;{nonce};{itemsReturned}</c>.
/// </summary>
/// <remarks>
/// The token never contains the queryId: the handler calculates the queryId from the query
/// (see <see cref="IQueryIdProvider"/>), so a client can't choose which stored result it reads.
/// </remarks>
/// <param name="Nonce">The random value of the paging run, or <see langword="null"/> when a session id is used.</param>
/// <param name="ItemsReturned">The number of items returned so far.</param>
internal sealed record PagedContinuationToken(
    string? Nonce,
    long ItemsReturned)
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
        if (parts.Length != 3 ||
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

        return new PagedContinuationToken(
            nonce.Length > 0 ? nonce : null,
            itemsReturned);
    }

    /// <summary>
    /// Writes the token as <c>v2;{nonce};{itemsReturned}</c>.
    /// </summary>
    /// <returns>The token.</returns>
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{Version};{Nonce};{ItemsReturned}");
}