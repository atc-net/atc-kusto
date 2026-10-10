namespace Atc.Kusto;

/// <summary>
/// Ingests data into Azure Data Explorer.
/// </summary>
/// <remarks>
/// <para>
/// Registered automatically by <c>ConfigureAzureDataExplorer(...)</c>. The connection must be configured
/// with a <c>HostAddress</c> and a <c>Credential</c>.
/// </para>
/// <para>
/// <b>Failures are returned, not thrown.</b> When the service rejects the request, or a network or
/// credential problem occurs, the call completes with <see cref="KustoIngestionStatus.Failed"/> and an
/// <see cref="KustoIngestionResult.ErrorMessage"/>. Always check <see cref="KustoIngestionResult.IsSuccess"/>,
/// or call <see cref="KustoIngestionResult.EnsureSuccess"/> to throw a <see cref="KustoIngestionException"/>
/// instead. Only invalid arguments, a connection not configured for ingestion, and cancellation throw.
/// </para>
/// <para>
/// Ingestion is at-least-once, so prefer idempotent tables or caller-side de-duplication.
/// </para>
/// </remarks>
public interface IKustoIngestor
{
    /// <summary>
    /// Ingests in-memory rows, serialized to multijson (one JSON object per line).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="KustoIngestTarget.Format"/> must be <see cref="KustoIngestFormat.Json"/> or
    /// <see cref="KustoIngestFormat.MultiJson"/>, so that the declared format matches the bytes produced.
    /// </para>
    /// <para>
    /// By default, property names are written in camelCase (<c>SerialNumber</c> → <c>"serialNumber"</c>).
    /// The table's JSON ingestion mapping paths are case-sensitive and must match (<c>$.serialNumber</c>);
    /// a mismatch does not fail, the column is just left empty. Use <paramref name="serializerOptions"/>
    /// or <c>[JsonPropertyName]</c> to change names.
    /// </para>
    /// <para>
    /// The rows are enumerated once and buffered in memory; validation happens before enumeration.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="rows">The rows to ingest. An empty sequence returns <see cref="KustoIngestionStatus.Skipped"/>.</param>
    /// <param name="target">The ingestion target.</param>
    /// <param name="serializerOptions">
    /// Optional serializer options; when <see langword="null"/>, the library's Kusto JSON options are used
    /// (camelCase names, enums as strings, Kusto boolean and <see cref="DateOnly"/> handling). Supplied
    /// options replace those defaults entirely.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The result. <see cref="KustoIngestionResult.Status"/> tells how far the data got:
    /// <see cref="KustoIngestionStatus.Succeeded"/> (in the table), <see cref="KustoIngestionStatus.Queued"/>
    /// (accepted, processed later), <see cref="KustoIngestionStatus.Skipped"/> (no rows) or
    /// <see cref="KustoIngestionStatus.Failed"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the target is invalid, for example a missing table name, a JSON format without a
    /// mapping reference, a non-JSON format, no resolvable database, or more than 10 MB for
    /// <see cref="IngestionMode.Streaming"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection has no HostAddress or Credential.</exception>
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
    /// The stream must be seekable; the data from its current position to the end is ingested. The
    /// stream is owned by the caller: it is neither disposed nor closed.
    /// </remarks>
    /// <param name="data">
    /// The payload, matching <see cref="KustoIngestTarget.Format"/>. Nothing remaining after the current
    /// position returns <see cref="KustoIngestionStatus.Skipped"/>.
    /// </param>
    /// <param name="target">The ingestion target.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result; see <see cref="KustoIngestionResult.Status"/> and <see cref="KustoIngestionResult.IsSuccess"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="data"/> is not seekable, or the target is invalid, for example a missing
    /// table name, a JSON format without a mapping reference, no resolvable database, or more than 10 MB
    /// for <see cref="IngestionMode.Streaming"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection has no HostAddress or Credential.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<KustoIngestionResult> IngestAsync(
        Stream data,
        KustoIngestTarget target,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests from a blob.
    /// </summary>
    /// <remarks>
    /// The cluster reads the blob itself, so the URI must be cluster-readable, either via a SAS token or
    /// by granting the ingestion identity Storage Blob Data Reader. Compression is inferred by the service,
    /// for example from a <c>.gz</c> extension.
    /// </remarks>
    /// <param name="blobUri">The absolute blob URI, including a SAS token when one is used.</param>
    /// <param name="target">The ingestion target.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result; see <see cref="KustoIngestionResult.Status"/> and <see cref="KustoIngestionResult.IsSuccess"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="blobUri"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="blobUri"/> is relative, or the target is invalid, for example a missing
    /// table name, a JSON format without a mapping reference, or no resolvable database.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection has no HostAddress or Credential.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<KustoIngestionResult> IngestFromBlobAsync(
        Uri blobUri,
        KustoIngestTarget target,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a tracked ingestion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see cref="KustoIngestionResult.OperationHandle"/> returned by an ingestion with
    /// <see cref="KustoIngestTarget.EnableTracking"/> set. Queued data is processed in batches, so poll until
    /// <see cref="KustoIngestionOperationResult.IsCompleted"/> is <see langword="true"/> — typically minutes,
    /// depending on the table's batching policy. An ingestion that was streamed reports
    /// <see cref="KustoIngestionOperationStatus.Succeeded"/> without contacting the cluster.
    /// </para>
    /// <para>
    /// Unlike the ingest methods, a failure to <em>check</em> the status (for example the cluster is
    /// unreachable) throws <see cref="KustoIngestionException"/>; the check only reads, so it is safe to retry.
    /// The ingestion's own failure is reported as <see cref="KustoIngestionOperationStatus.Failed"/>.
    /// </para>
    /// </remarks>
    /// <param name="operationHandle">The handle from <see cref="KustoIngestionResult.OperationHandle"/>.</param>
    /// <param name="connectionName">
    /// The named connection the ingestion was made on; <see langword="null"/> for the default connection. The
    /// handle identifies database and table, not the cluster.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation's state, counts and any per-source errors.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operationHandle"/> is empty or not a valid handle.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection has no HostAddress or Credential.</exception>
    /// <exception cref="KustoIngestionException">Thrown when the status could not be retrieved.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    Task<KustoIngestionOperationResult> GetIngestionStatusAsync(
        string operationHandle,
        string? connectionName = null,
        CancellationToken cancellationToken = default);
}