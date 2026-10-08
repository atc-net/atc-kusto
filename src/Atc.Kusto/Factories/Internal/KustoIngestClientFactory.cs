namespace Atc.Kusto.Factories.Internal;

/// <inheritdoc />
internal sealed class KustoIngestClientFactory : IKustoIngestClientFactory
{
    /// <inheritdoc />
    public IKustoIngestClient Create(
        Uri clusterUri,
        TokenCredential credential,
        IList<Uri> uploadContainers,
        IngestionMode mode)
        => new KustoIngestClient(
            clusterUri,
            credential,
            uploadContainers,
            mode);
}