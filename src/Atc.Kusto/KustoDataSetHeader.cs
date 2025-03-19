namespace Atc.Kusto;

/// <summary>
/// Represents the header information for a dataset.
/// </summary>
public class KustoDataSetHeader
{
    public string Version { get; set; } = string.Empty;

    public bool IsProgressive { get; set; }
}