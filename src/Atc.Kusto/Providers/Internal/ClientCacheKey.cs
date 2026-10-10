namespace Atc.Kusto.Providers.Internal;

internal readonly record struct ClientCacheKey(
    string? ConnectionName,
    string? DatabaseName);