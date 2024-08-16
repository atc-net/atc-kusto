namespace Atc.Kusto.Providers.Internal;

/// <inheritdoc />
internal sealed class QueryIdProvider : IQueryIdProvider
{
    /// <inheritdoc />
    public string Create(
        Type queryType,
        string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(queryType);

        var finalSessionId = sessionId switch
        {
            not null => sessionId,
            _ => Guid.NewGuid().ToString("N"),
        };

        return string
            .Concat(queryType.Name, finalSessionId)
            .ToAlphaNumeric();
    }
}