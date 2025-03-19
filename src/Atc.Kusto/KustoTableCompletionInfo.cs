namespace Atc.Kusto;

public class KustoTableCompletionInfo
{
    public string TableName { get; set; } = string.Empty;

    public long RowCount { get; set; }

    public string? Exception { get; set; }
}