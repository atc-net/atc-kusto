namespace Atc.Kusto.CLI.Rendering;

/// <summary>
/// Materializes an <see cref="System.Data.IDataReader"/> into column names and string rows
/// suitable for rendering.
/// </summary>
public static class DataReaderMaterializer
{
    /// <summary>
    /// Reads all columns and rows from the data reader into string lists.
    /// </summary>
    /// <param name="reader">The data reader to materialize.</param>
    /// <returns>A tuple of column names and row data.</returns>
    public static (List<string> Columns, List<string[]> Rows) Materialize(System.Data.IDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var columns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        var rows = new List<string[]>();
        while (reader.Read())
        {
            var row = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
            }

            rows.Add(row);
        }

        return (columns, rows);
    }
}