namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// The name, kind and columns of one table in a progressive query result.
/// Kept by table id, so the rows that arrive in later <c>TableFragment</c> frames can be read.
/// </summary>
internal sealed class FrameTable
{
    private FrameTable(
        string name,
        WellKnownDataSet kind,
        string[] columnNames,
        Type[] columnTypes)
    {
        Name = name;
        Kind = kind;
        ColumnNames = columnNames;
        ColumnTypes = columnTypes;
    }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the table kind, e.g. <see cref="WellKnownDataSet.PrimaryResult"/>.
    /// </summary>
    public WellKnownDataSet Kind { get; }

    /// <summary>
    /// Gets the column names, in column order.
    /// </summary>
    public IReadOnlyList<string> ColumnNames { get; }

    /// <summary>
    /// Gets the CLR column types, in column order.
    /// </summary>
    public IReadOnlyList<Type> ColumnTypes { get; }

    /// <summary>
    /// Creates the table from a <c>TableHeader</c> frame.
    /// </summary>
    /// <remarks>
    /// Uses the SDK's <c>ToEmptyDataTable()</c> only so the SDK translates the Kusto column types ("guid", "real")
    /// into .NET types (<see cref="Guid"/>, <see cref="double"/>). The names and types are copied out and the empty
    /// table is disposed; no rows are ever added to it.
    /// </remarks>
    /// <param name="frame">The frame with the table schema.</param>
    /// <returns>The table.</returns>
    public static FrameTable FromSchemaFrame(
        ProgressiveDataSetDataTableSchemaFrame frame)
    {
        using var schema = frame.ToEmptyDataTable();

        var columnNames = new string[schema.Columns.Count];
        var columnTypes = new Type[schema.Columns.Count];
        for (var i = 0; i < schema.Columns.Count; i++)
        {
            columnNames[i] = schema.Columns[i].ColumnName;
            columnTypes[i] = schema.Columns[i].DataType;
        }

        return new FrameTable(frame.TableName, frame.TableKind, columnNames, columnTypes);
    }

    /// <summary>
    /// Creates the table from a <c>DataTable</c> frame, using the columns of its reader.
    /// </summary>
    /// <param name="frame">The frame with the whole table.</param>
    /// <returns>The table.</returns>
    public static FrameTable FromDataTableFrame(
        ProgressiveDataSetDataTableFrame frame)
    {
        var reader = frame.TableData;

        var columnNames = new string[reader.FieldCount];
        var columnTypes = new Type[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columnNames[i] = reader.GetName(i);
            columnTypes[i] = reader.GetFieldType(i);
        }

        return new FrameTable(frame.TableName, frame.TableKind, columnNames, columnTypes);
    }

    /// <summary>
    /// Creates a record for one row of a <c>TableFragment</c> frame, with each value converted to its column type.
    /// </summary>
    /// <param name="values">
    /// The values as the SDK sends them. They are copied, so the array can be reused for the next row.
    /// </param>
    /// <returns>The record.</returns>
    public FrameRecord CreateRecord(object?[] values)
    {
        var convertedValues = new object[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            convertedValues[i] = FrameValueTypeConverter.ConvertToColumnType(values[i], ColumnTypes[i]) ?? DBNull.Value;
        }

        return new FrameRecord(this, convertedValues);
    }
}