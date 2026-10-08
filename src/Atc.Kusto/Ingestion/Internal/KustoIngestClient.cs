namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// The Ingest V2 backed implementation of <see cref="IKustoIngestClient"/>.
/// </summary>
/// <remarks>
/// This is the only file in the library that references Kusto Ingest V2 source, property and
/// operation types, so SDK churn stays contained here.
/// </remarks>
internal sealed class KustoIngestClient : IKustoIngestClient
{
    public KustoIngestClient(
        Uri clusterUri,
        TokenCredential credential,
        IList<Uri> uploadContainers,
        IngestionMode mode)
    {
        ArgumentNullException.ThrowIfNull(clusterUri);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(uploadContainers);
    }

    public Task<KustoIngestionResult> IngestStreamAsync(
        Stream data,
        KustoIngestFormat format,
        string database,
        string table,
        string? mappingReference,
        bool enableTracking,
        IngestionMode mode,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<KustoIngestionResult> IngestBlobAsync(
        Uri blobUri,
        KustoIngestFormat format,
        string database,
        string table,
        string? mappingReference,
        bool enableTracking,
        IngestionMode mode,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public void Dispose()
    {
        // Filled in by Task 8, alongside the V2 client it will own.
    }
}