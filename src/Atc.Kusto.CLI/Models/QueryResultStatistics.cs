namespace Atc.Kusto.CLI.Models;

public sealed class QueryResultStatistics
{
    public long? RowCount { get; init; }

    public double? SizeKb { get; init; }
}