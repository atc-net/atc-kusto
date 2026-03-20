namespace Atc.Kusto.CLI.Models;

public sealed class QueryCpuStatistics
{
    public string? Total { get; init; }

    public string? QueryExecution { get; init; }

    public string? QueryPlanning { get; init; }
}