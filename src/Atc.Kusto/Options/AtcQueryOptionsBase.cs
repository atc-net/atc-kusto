namespace Atc.Kusto.Options;

public abstract class AtcQueryOptionsBase
{
    /// <summary>
    /// Limits query results to a specified number of records.
    /// </summary>
    public long? QueryTakeMaxRecords { get; set; }

    /// <summary>
    /// Enables suppressing truncation of the query results returned to the caller.
    /// When enabled, the query results are not limited to 500000 rows or 64MB in size.
    /// </summary>
    /// <remarks>
    /// If either <see cref="QueryTakeMaxRecords"/>, <see cref="TruncationMaxRecords"/> or <see cref="TruncationMaxSize"/> is set <see cref="NoTruncation"/> will be ignored.
    /// </remarks>
    public bool NoTruncation { get; set; }

    /// <summary>
    /// Overrides the default maximum number of records a query is allowed to return to the caller(truncation).
    /// </summary>
    public long? TruncationMaxRecords { get; set; }

    /// <summary>
    /// Overrides the default maximum data size a query is allowed to return to the caller(truncation).
    /// </summary>
    public long? TruncationMaxSize { get; set; }
}