// ReSharper disable CheckNamespace
namespace Atc.Kusto;

/// <summary>
/// Provides extension methods for reading and converting data from an <see cref="IDataReader"/> to objects using System.Text.Json.
/// These methods simplify the process of converting database records into strongly-typed objects.
/// Note: Newtonsoft.Json.Linq is retained for handling <see cref="JToken"/> objects returned by the Kusto SDK for dynamic fields.
/// </summary>
public static class DataReaderExtensions
{
    /// <summary>
    /// Reads all rows from the <see cref="IDataReader"/> and converts them into an array of strongly-typed objects of type <typeparamref name="T"/>.
    /// The conversion is handled using System.Text.Json for optimal performance.
    /// </summary>
    /// <typeparam name="T">The type of objects to convert the data into.</typeparam>
    /// <param name="reader">The <see cref="IDataReader"/> from which to read the data.</param>
    /// <param name="options">Optional JSON serializer options. If not provided, default options with enum string conversion, case-insensitive property matching, and number string reading will be used.</param>
    /// <returns>An array of objects of type <typeparamref name="T"/> representing the data read from the reader.</returns>
    public static T[] ReadObjects<T>(
        this IDataReader reader,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(reader);

        options ??= KustoJsonSerializerOptions.Default;

        var buffer = new ArrayBufferWriter<byte>();
        using var doc = new Utf8JsonWriter(buffer);
        var results = new List<T>();

        while (reader.Read())
        {
            results.Add(ConvertRow<T>(reader, options, buffer, doc));
        }

        return [.. results];
    }

    /// <summary>
    /// Converts the current row of the <see cref="IDataRecord"/> into a strongly-typed object of type <typeparamref name="T"/>.
    /// Uses the same conversion as <see cref="ReadObjects{T}"/>, so a row maps the same way in queries and streaming queries.
    /// </summary>
    /// <typeparam name="T">The type of object to convert the row into.</typeparam>
    /// <param name="record">The record (for example an <see cref="IDataReader"/> positioned on a row) to read.</param>
    /// <param name="options">Optional JSON serializer options. If not provided, default options with enum string conversion, case-insensitive property matching, and number string reading will be used.</param>
    /// <returns>An object of type <typeparamref name="T"/> representing the row.</returns>
    public static T ReadObject<T>(
        this IDataRecord record,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(record);

        options ??= KustoJsonSerializerOptions.Default;

        var buffer = new ArrayBufferWriter<byte>();
        using var doc = new Utf8JsonWriter(buffer);

        return ConvertRow<T>(record, options, buffer, doc);
    }

    /// <summary>
    /// Advances the <see cref="IDataReader"/> to the next result set, if available, and converts the resulting rows into an array of strongly-typed objects of type <typeparamref name="T"/>.
    /// This method is useful for handling multiple result sets from a single query execution.
    /// </summary>
    /// <typeparam name="T">The type of objects to convert the data into.</typeparam>
    /// <param name="reader">The <see cref="IDataReader"/> from which to read the data.</param>
    /// <returns>An array of objects of type <typeparamref name="T"/> representing the data from the next result set, or an empty array if there is no next result set.</returns>
    public static T[] ReadObjectsFromNextResult<T>(this IDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return reader.NextResult()
            ? reader.ReadObjects<T>()
            : [];
    }

    /// <summary>
    /// Writes the current row as a JSON object into <paramref name="buffer"/> and deserializes it to <typeparamref name="T"/>.
    /// The buffer and writer are reused between rows by <see cref="ReadObjects{T}"/>.
    /// </summary>
    private static T ConvertRow<T>(
        IDataRecord record,
        JsonSerializerOptions options,
        ArrayBufferWriter<byte> buffer,
        Utf8JsonWriter doc)
    {
        buffer.Clear();
        doc.Reset(buffer);

        doc.WriteStartObject();

        for (var i = 0; i < record.FieldCount; i++)
        {
            var value = record.GetValue(i);

            var name = record.GetName(i);
            if (options.PropertyNamingPolicy is { } np)
            {
                name = np.ConvertName(name);
            }

            doc.WritePropertyName(name);

            switch (value)
            {
                case JToken token:
                    doc.WriteRawValue(token.ToString(Newtonsoft.Json.Formatting.None));
                    break;
                case DBNull:
                    doc.WriteNullValue();
                    break;
                case SqlDecimal sd:
                    doc.WriteNumberValue(sd.ToDecimal());
                    break;
                default:
                    JsonSerializer.Serialize(doc, value, options);
                    break;
            }
        }

        doc.WriteEndObject();
        doc.Flush();

        var deserializedObject = JsonSerializer.Deserialize<T>(buffer.WrittenSpan, options);
        if (deserializedObject is null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize DataReader row to type {typeof(T).Name}. " +
                $"This may indicate corrupted data or an incompatible type mapping.");
        }

        return deserializedObject;
    }
}