namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// Serializes in-memory rows to the multijson wire format.
/// </summary>
/// <remarks>
/// Multijson is one JSON object per line. The ingestor passes the connection's serializer settings
/// by default, the same ones used to read query results, so what is written round-trips with what
/// is read back.
/// </remarks>
internal static class MultiJsonSerializer
{
    /// <summary>
    /// Serializes the rows into a rewound <see cref="MemoryStream"/>.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="rows">The rows to serialize. Enumerated exactly once.</param>
    /// <param name="serializerOptions">The serializer settings.</param>
    /// <param name="cancellationToken">Checked before each row, so a large batch can be abandoned promptly.</param>
    /// <returns>A stream positioned at 0; empty when <paramref name="rows"/> is empty.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public static MemoryStream Serialize<T>(
        IEnumerable<T> rows,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();

        try
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytes = JsonSerializer.SerializeToUtf8Bytes(row, serializerOptions);
                stream.Write(bytes, 0, bytes.Length);
                stream.WriteByte((byte)'\n');
            }
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        stream.Position = 0;
        return stream;
    }
}