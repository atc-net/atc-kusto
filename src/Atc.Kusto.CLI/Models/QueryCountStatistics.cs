namespace Atc.Kusto.CLI.Models;

public sealed class QueryCountStatistics
{
    public long? Scanned { get; init; }

    public long? Total { get; init; }
}