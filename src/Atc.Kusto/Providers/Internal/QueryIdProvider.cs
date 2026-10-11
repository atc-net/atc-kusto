namespace Atc.Kusto.Providers.Internal;

/// <inheritdoc />
internal sealed class QueryIdProvider : IQueryIdProvider
{
    private const int HashLength = 32;

    /// <inheritdoc />
    public string Create(
        IKustoScript query,
        string? sessionId,
        string? nonce)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        var hash = Hash(queryType.FullName, sessionId, nonce, CreateFingerprint(query));

        return string
            .Concat(queryType.Name, hash)
            .ToAlphanumeric();
    }

    /// <summary>
    /// Hashes the query type, the query text and the parameters (sorted by name).
    /// </summary>
    private static string CreateFingerprint(IKustoScript query)
    {
        var values = new List<string?>
        {
            query.GetType().FullName,
            query.GetQueryText(),
        };

        foreach (var parameter in query
            .GetCslParameters()
            .OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            values.Add(parameter.Key);
            values.Add(parameter.Value);
        }

        return Hash([.. values]);
    }

    /// <summary>
    /// SHA-256 over the values, each written as <c>length:value</c> (<c>-1:</c> for <see langword="null"/>), so
    /// different values never produce the same input (<c>"ab" + "c"</c> versus <c>"a" + "bc"</c>).
    /// </summary>
    private static string Hash(params string?[] values)
    {
        var builder = new StringBuilder();
        foreach (var value in values)
        {
            if (value is null)
            {
                builder.Append("-1:");
                continue;
            }

            builder
                .Append(value.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(value);
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));

        return Convert.ToHexStringLower(hash)[..HashLength];
    }
}