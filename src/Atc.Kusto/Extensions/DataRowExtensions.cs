// ReSharper disable CheckNamespace
namespace Atc.Kusto;

/// <summary>
/// Provides extension methods for mapping a <see cref="DataRow"/> to a strongly-typed object.
/// </summary>
public static class DataRowExtensions
{
    /// <summary>
    /// Maps the specified <see cref="DataRow"/> to an object of type <typeparamref name="T"/>.
    /// The mapping is performed by converting the row's values to JSON using System.Text.Json
    /// and then deserializing to the target type.
    /// Special handling is provided for SqlDecimal values returned by Kusto to ensure proper conversion.
    /// </summary>
    /// <typeparam name="T">The target type to which the row is mapped.</typeparam>
    /// <param name="row">The data row to map.</param>
    /// <param name="options">Optional JSON serializer options. If not provided, default options with enum string conversion and case-insensitive property matching will be used.</param>
    /// <returns>An instance of type <typeparamref name="T"/> created from the DataRow.</returns>
    public static T? MapDataRow<T>(
        this DataRow row,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(row);

        options ??= KustoJsonSerializerOptions.Default;

        // Convert the DataRow to a dictionary using its columns.
        // Pre-process values to handle SqlDecimal, DBNull, and JToken since System.Text.Json doesn't support them directly.
        var dict = row.Table.Columns
            .Cast<DataColumn>()
            .ToDictionary(
                col => col.ColumnName,
                col =>
                {
                    var value = row[col];
                    return value switch
                    {
                        SqlDecimal sd => sd.ToDecimal(),
                        DBNull => null,
                        JToken token => JsonSerializer.Deserialize<object>(
                            token.ToString(Newtonsoft.Json.Formatting.None),
                            options),
                        _ => value,
                    };
                },
                StringComparer.Ordinal);

        // Serialize to JSON and deserialize to the target type using System.Text.Json.
        var json = JsonSerializer.Serialize(dict, options);
        return JsonSerializer.Deserialize<T>(json, options);
    }
}