// ReSharper disable CheckNamespace
namespace Atc.Kusto;

/// <summary>
/// Provides extension methods for mapping a <see cref="DataRow"/> to a strongly-typed object.
/// </summary>
public static class DataRowExtensions
{
    /// <summary>
    /// Maps the specified <see cref="DataRow"/> to an object of type <typeparamref name="T"/>.
    /// The mapping is performed by converting the row's values to a JSON object using a shared serializer.
    /// </summary>
    /// <typeparam name="T">The target type to which the row is mapped.</typeparam>
    /// <param name="row">The data row to map.</param>
    /// <returns>An instance of type <typeparamref name="T"/> created from the DataRow.</returns>
    public static T? MapDataRow<T>(this DataRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        // Convert the DataRow to a dictionary using its columns.
        var dict = row.Table.Columns
            .Cast<DataColumn>()
            .ToDictionary(col => col.ColumnName, col => row[col], StringComparer.Ordinal);

        // Create a JObject from the dictionary using the shared serializer.
        var jObject = Newtonsoft.Json.Linq.JObject.FromObject(dict, KustoJsonSerializerHelper.Serializer);

        return jObject.ToObject<T>(KustoJsonSerializerHelper.Serializer);
    }
}