namespace Atc.Kusto.Providers.Internal;

/// <summary>
/// Cache key for ingestion clients.
/// </summary>
/// <remarks>
/// Unlike <see cref="ClientCacheKey"/> this carries no database, because an Ingest V2 client is
/// built from the cluster URI, credential and mode alone; the database is passed per call.
/// </remarks>
internal readonly record struct IngestClientCacheKey(
    string? ConnectionName,
    IngestionMode Mode);