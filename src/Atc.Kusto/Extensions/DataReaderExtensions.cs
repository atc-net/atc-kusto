// ReSharper disable CheckNamespace
namespace Atc.Kusto;

/// <summary>
/// Provides extension methods for reading and converting data from an <see cref="IDataReader"/> to objects using Newtonsoft.Json.
/// These methods simplify the process of converting database records into strongly-typed objects.
/// </summary>
public static class DataReaderExtensions
{
    /// <summary>
    /// A custom JSON serializer configured with a converter that handles the deserialization
    /// of objects from Newtonsoft.Json to System.Text.Json format.
    /// </summary>
    private static readonly Newtonsoft.Json.JsonSerializer Serializer = Newtonsoft.Json.JsonSerializer.CreateDefault(
        new()
        {
            Converters = { new NewtonsoftObjectConverter() },
        });

    /// <summary>
    /// Reads all rows from the <see cref="IDataReader"/> and converts them into an array of strongly-typed objects of type <typeparamref name="T"/>.
    /// The conversion is handled using Newtonsoft.Json with a custom converter to facilitate the transition between Newtonsoft.Json and System.Text.Json.
    /// </summary>
    /// <typeparam name="T">The type of objects to convert the data into.</typeparam>
    /// <param name="reader">The <see cref="IDataReader"/> from which to read the data.</param>
    /// <returns>An array of objects of type <typeparamref name="T"/> representing the data read from the reader.</returns>
    public static T[] ReadObjects<T>(
        this IDataReader reader)
        => reader
            .ToJObjects()
            .Select(o => o.ToObject<T>(Serializer))
            .OfType<T>()
            .ToArray();

    /// <summary>
    /// Advances the <see cref="IDataReader"/> to the next result set, if available, and converts the resulting rows into an array of strongly-typed objects of type <typeparamref name="T"/>.
    /// This method is useful for handling multiple result sets from a single query execution.
    /// </summary>
    /// <typeparam name="T">The type of objects to convert the data into.</typeparam>
    /// <param name="reader">The <see cref="IDataReader"/> from which to read the data.</param>
    /// <returns>An array of objects of type <typeparamref name="T"/> representing the data from the next result set, or an empty array if there is no next result set.</returns>
    public static T[] ReadObjectsFromNextResult<T>(
        this IDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return reader.NextResult()
            ? reader.ReadObjects<T>()
            : [];
    }
}