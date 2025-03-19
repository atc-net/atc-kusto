namespace Atc.Kusto;

/// <summary>
/// Represents the query completion summary.
/// </summary>
public class KustoResultCompletion
{
    public bool HasErrors { get; set; }

    public string? ErrorMessage { get; set; }

    public IList<KustoTableCompletionInfo>? TableCompletions { get; } = [];
}