namespace Atc.Kusto.Providers.Internal;

/// <summary>
/// Provides cached ingestion clients, one per connection and mode.
/// </summary>
/// <remarks>
/// This is deliberately separate from <see cref="IKustoClientProvider"/>: that interface is
/// public, and a public interface cannot expose the internal <see cref="IKustoIngestClient"/>
/// seam. Keeping ingestion on its own internal interface also leaves the existing query and
/// command surface untouched.
/// </remarks>
internal interface IKustoIngestClientProvider
{
    /// <summary>
    /// Retrieves, creating and caching on first use, an ingestion client for the given mode.
    /// </summary>
    /// <remarks>
    /// Clients are cached by connection and mode only. The database is a per-call argument to the
    /// ingest operation, so including it in the key would spawn redundant identical clients.
    /// </remarks>
    /// <param name="mode">The ingestion mode the client is built for.</param>
    /// <param name="connectionName">The optional named connection; <see langword="null"/> selects the default.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection is not configured with both a HostAddress and a Credential,
    /// which the Ingest V2 builders require.
    /// </exception>
    IKustoIngestClient GetIngestClient(
        IngestionMode mode,
        string? connectionName = null);
}