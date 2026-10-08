namespace Atc.Kusto.Options;

public class AtcKustoOptions
{
    public Uri? HostAddress { get; set; }

    public string? DatabaseName { get; set; }

    public TokenCredential? Credential { get; set; }

    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the ingestion mode used when a request does not specify one.
    /// </summary>
    public IngestionMode DefaultIngestionMode { get; set; } = IngestionMode.ManagedStreaming;

    /// <summary>
    /// Gets the blob containers that queued and managed-streaming ingestion uploads to.
    /// </summary>
    /// <remarks>
    /// Supply your own, privately reachable containers when the cluster sits behind a private
    /// endpoint. When empty, the SDK's default Kusto-internal storage is used. The connection's
    /// credential is reused for these containers, so that identity needs Storage Blob Data
    /// Contributor on them.
    /// </remarks>
    public IList<Uri> IngestUploadContainers { get; } = new List<Uri>();
}