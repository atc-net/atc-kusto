namespace Atc.Kusto;

/// <summary>
/// Represents a table column.
/// </summary>
public record KustoColumn(
    string Name,
    string Type);