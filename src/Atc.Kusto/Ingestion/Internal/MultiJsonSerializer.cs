namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// Serializes in-memory rows to the multijson wire format.
/// </summary>
/// <remarks>
/// Multijson is one JSON object per line. Using the library's shared serializer options by
/// default keeps ingestion symmetric with how results are deserialized, so what is written
/// round-trips with what is read back.
/// </remarks>
internal static class MultiJsonSerializer
{
    /// <summary>
    /// Serializes the rows into a rewound <see cref="MemoryStream"/>.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="rows">The rows to serialize.</param>
    /// <param name="serializerOptions">
    /// Optional serializer override. When <see langword="null"/>, the library's shared Kusto
    /// options are used, which emit verbatim PascalCase property names.
    /// </param>
    public static MemoryStream Serialize<T>(
        IEnumerable<T> rows,
        JsonSerializerOptions? serializerOptions)
    {
        var options = serializerOptions ?? KustoJsonSerializerOptions.Default;
        var stream = new MemoryStream();

        foreach (var row in rows)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(row, options);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte((byte)'\n');
        }

        stream.Position = 0;
        return stream;
    }
}