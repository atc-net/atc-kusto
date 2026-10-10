namespace Atc.Kusto.Handlers.Internal;

/// <summary>
/// One row of a <c>TableFragment</c> frame, read through <see cref="IDataRecord"/>.
/// Null values are <see cref="DBNull.Value"/>, like in any other <see cref="IDataRecord"/>.
/// </summary>
internal sealed class FrameRecord : IDataRecord
{
    private readonly FrameTable table;
    private readonly object[] values;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameRecord"/> class.
    /// </summary>
    /// <param name="table">The table the row belongs to.</param>
    /// <param name="values">The values, already converted to the column types.</param>
    public FrameRecord(
        FrameTable table,
        object[] values)
    {
        this.table = table;
        this.values = values;
    }

    /// <summary>
    /// Gets the table the row belongs to.
    /// </summary>
    public FrameTable Table => table;

    /// <inheritdoc />
    public int FieldCount => values.Length;

    /// <inheritdoc />
    public object this[int i] => GetValue(i);

    /// <inheritdoc />
    public object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc />
    public string GetName(int i) => table.ColumnNames[i];

    /// <inheritdoc />
    public string GetDataTypeName(int i) => table.ColumnTypes[i].Name;

    /// <inheritdoc />
    public Type GetFieldType(int i) => table.ColumnTypes[i];

    /// <inheritdoc />
    public object GetValue(int i) => values[i];

    /// <inheritdoc />
    public int GetValues(object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var count = System.Math.Min(values.Length, this.values.Length);
        Array.Copy(this.values, values, count);

        return count;
    }

    /// <summary>
    /// Returns the index of the named column: an exact match first, otherwise a case-insensitive one.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The column index.</returns>
    /// <exception cref="ArgumentException">Thrown when no column has that name.</exception>
    public int GetOrdinal(string name)
    {
        for (var i = 0; i < table.ColumnNames.Count; i++)
        {
            if (string.Equals(table.ColumnNames[i], name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        for (var i = 0; i < table.ColumnNames.Count; i++)
        {
            if (string.Equals(table.ColumnNames[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new ArgumentException($"Column '{name}' was not found.", nameof(name));
    }

    /// <inheritdoc />
    public bool IsDBNull(int i) => values[i] is DBNull;

    /// <inheritdoc />
    public bool GetBoolean(int i) => (bool)values[i];

    /// <inheritdoc />
    public byte GetByte(int i) => (byte)values[i];

    /// <inheritdoc />
    public char GetChar(int i) => (char)values[i];

    /// <inheritdoc />
    public Guid GetGuid(int i) => (Guid)values[i];

    /// <inheritdoc />
    public short GetInt16(int i) => (short)values[i];

    /// <inheritdoc />
    public int GetInt32(int i) => (int)values[i];

    /// <inheritdoc />
    public long GetInt64(int i) => (long)values[i];

    /// <inheritdoc />
    public float GetFloat(int i) => (float)values[i];

    /// <inheritdoc />
    public double GetDouble(int i) => (double)values[i];

    /// <inheritdoc />
    public string GetString(int i) => (string)values[i];

    /// <summary>
    /// Gets the value as a <see cref="decimal"/>. Kusto decimals arrive as <see cref="SqlDecimal"/>.
    /// </summary>
    /// <param name="i">The column index.</param>
    /// <returns>The value.</returns>
    public decimal GetDecimal(int i)
        => values[i] is SqlDecimal sqlDecimal
            ? sqlDecimal.Value
            : (decimal)values[i];

    /// <inheritdoc />
    public DateTime GetDateTime(int i) => (DateTime)values[i];

    /// <inheritdoc />
    /// <remarks>Not supported: Kusto has no binary columns.</remarks>
    public long GetBytes(
        int i,
        long fieldOffset,
        byte[]? buffer,
        int bufferoffset,
        int length)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <remarks>Not supported: use <see cref="GetString"/>.</remarks>
    public long GetChars(
        int i,
        long fieldoffset,
        char[]? buffer,
        int bufferoffset,
        int length)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <remarks>Not supported: Kusto has no nested tables.</remarks>
    public IDataReader GetData(int i)
        => throw new NotSupportedException();
}