namespace Atc.Kusto.Providers.Internal;

internal record ClientCacheKey(
    string? ConnectionName,
    string? DatabaseName);