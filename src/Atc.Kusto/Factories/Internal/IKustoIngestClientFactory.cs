namespace Atc.Kusto.Factories.Internal;

/// <summary>
/// Creates ingestion clients for a cluster, credential and mode.
/// </summary>
/// <remarks>
/// Kept separate from the provider so that client construction can be substituted in tests,
/// for example to assert that concurrent first calls create exactly one client.
/// </remarks>
internal interface IKustoIngestClientFactory
{
    /// <summary>
    /// Creates a new ingestion client. The caller owns and disposes it.
    /// </summary>
    /// <param name="clusterUri">The cluster URI.</param>
    /// <param name="credential">The credential used for the cluster and any upload containers.</param>
    /// <param name="uploadContainers">Optional user-supplied upload containers; empty uses the SDK default.</param>
    /// <param name="mode">The ingestion mode the client is built for.</param>
    /// <returns>A new <see cref="IKustoIngestClient"/>.</returns>
    IKustoIngestClient Create(
        Uri clusterUri,
        TokenCredential credential,
        IList<Uri> uploadContainers,
        IngestionMode mode);
}