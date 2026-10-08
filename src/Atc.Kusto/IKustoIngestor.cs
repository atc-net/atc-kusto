namespace Atc.Kusto;

/// <summary>
/// Ingests data into Azure Data Explorer.
/// </summary>
/// <remarks>
/// Registered automatically by <c>ConfigureAzureDataExplorer(...)</c>. Operational failures are
/// reported on <see cref="KustoIngestionResult"/>; only argument validation and cancellation
/// throw. Ingestion is at-least-once, so prefer idempotent tables or caller-side de-duplication.
/// </remarks>
public interface IKustoIngestor
{
    /// <summary>
    /// Ingests in-memory rows, serialized to multijson.
    /// </summary>
    /// <remarks>
    /// <see cref="KustoIngestTarget.Format"/> must be <see cref="KustoIngestFormat.Json"/> or
    /// <see cref="KustoIngestFormat.MultiJson"/>, so that the declared format matches the bytes
    /// produced.
    /// </remarks>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="rows">The rows to ingest.</param>
    /// <param name="target">The ingestion target.</param>
    /// <param name="serializerOptions">Optional serializer override; defaults to the library's Kusto JSON options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The result. <see cref="KustoIngestionResult.Status"/> tells how far the data got:
    /// <see cref="KustoIngestionStatus.Succeeded"/> (in the table), <see cref="KustoIngestionStatus.Queued"/>
    /// (accepted, processed later) or <see cref="KustoIngestionStatus.Failed"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the target is invalid, for example a missing table name, a JSON format without a
    /// mapping reference, a non-JSON format, or no resolvable database.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<KustoIngestionResult> IngestAsync<T>(
        IEnumerable<T> rows,
        KustoIngestTarget target,
        JsonSerializerOptions? serializerOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests data from a stream.
    /// </summary>
    /// <remarks>
    /// The stream is owned by the caller and is not disposed.
    /// </remarks>
    /// <param name="data">The payload, matching <see cref="KustoIngestTarget.Format"/>, positioned at the start of the data.</param>
    /// <param name="target">The ingestion target.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result; see <see cref="KustoIngestionResult.Status"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the target is invalid, for example a missing table name, a JSON format without a
    /// mapping reference, a payload over 10 MB for <see cref="IngestionMode.Streaming"/>, or no
    /// resolvable database.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<KustoIngestionResult> IngestAsync(
        Stream data,
        KustoIngestTarget target,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests from a blob.
    /// </summary>
    /// <remarks>
    /// The caller must ensure the URI is cluster-readable, either via a SAS token or by granting
    /// the ingestion identity Storage Blob Data Reader.
    /// </remarks>
    /// <param name="blobUri">The absolute blob URI, including a SAS token when one is used.</param>
    /// <param name="target">The ingestion target.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result; see <see cref="KustoIngestionResult.Status"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobUri"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the target is invalid, for example a missing table name, a JSON format without a
    /// mapping reference, or no resolvable database.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<KustoIngestionResult> IngestFromBlobAsync(
        Uri blobUri,
        KustoIngestTarget target,
        CancellationToken cancellationToken = default);
}