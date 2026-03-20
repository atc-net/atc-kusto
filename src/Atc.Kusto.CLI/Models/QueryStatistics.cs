namespace Atc.Kusto.CLI.Models;

/// <summary>
/// Represents query execution statistics extracted from Kusto response frames.
/// </summary>
public sealed class QueryStatistics
{
    public double? ExecutionTimeSec { get; init; }

    public QueryCpuStatistics? Cpu { get; init; }

    public double? MemoryPeakPerNodeMb { get; init; }

    public QueryCacheStatistics? Cache { get; init; }

    public QueryCountStatistics? Extents { get; init; }

    public QueryCountStatistics? Rows { get; init; }

    public QueryResultStatistics? Result { get; init; }

    /// <summary>
    /// Converts the statistics to a flat dictionary of key-value pairs for display.
    /// </summary>
    public IDictionary<string, string> ToDictionary()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (ExecutionTimeSec is not null)
        {
            result["ExecutionTimeSec"] = FormatNumber(ExecutionTimeSec.Value);
        }

        if (Cpu is not null)
        {
            if (Cpu.Total is not null)
            {
                result["Cpu.Total"] = Cpu.Total;
            }

            if (Cpu.QueryExecution is not null)
            {
                result["Cpu.QueryExecution"] = Cpu.QueryExecution;
            }

            if (Cpu.QueryPlanning is not null)
            {
                result["Cpu.QueryPlanning"] = Cpu.QueryPlanning;
            }
        }

        if (MemoryPeakPerNodeMb is not null)
        {
            result["MemoryPeakPerNodeMb"] = FormatNumber(MemoryPeakPerNodeMb.Value);
        }

        if (Cache is not null)
        {
            if (Cache.HotHitMb is not null)
            {
                result["Cache.HotHitMb"] = FormatNumber(Cache.HotHitMb.Value);
            }

            if (Cache.HotMissMb is not null)
            {
                result["Cache.HotMissMb"] = FormatNumber(Cache.HotMissMb.Value);
            }
        }

        if (Extents is not null)
        {
            if (Extents.Scanned is not null)
            {
                result["Extents.Scanned"] = Extents.Scanned.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (Extents.Total is not null)
            {
                result["Extents.Total"] = Extents.Total.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        if (Rows is not null)
        {
            if (Rows.Scanned is not null)
            {
                result["Rows.Scanned"] = Rows.Scanned.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (Rows.Total is not null)
            {
                result["Rows.Total"] = Rows.Total.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        if (Result is not null)
        {
            if (Result.RowCount is not null)
            {
                result["Result.RowCount"] = Result.RowCount.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (Result.SizeKb is not null)
            {
                result["Result.SizeKb"] = FormatNumber(Result.SizeKb.Value);
            }
        }

        return result;
    }

    private static string FormatNumber(double value)
        => value.ToString("0.##", CultureInfo.InvariantCulture);
}