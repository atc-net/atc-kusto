namespace Atc.Kusto.CLI.Models;

public sealed class QueryCacheStatistics
{
    public double? HotHitMb { get; init; }

    public double? HotMissMb { get; init; }
}