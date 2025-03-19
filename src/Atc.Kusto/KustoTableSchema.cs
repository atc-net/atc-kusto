namespace Atc.Kusto;

/// <summary>
/// Represents a table schema.
/// </summary>
public record KustoTableSchema(
    string TableName,
    IReadOnlyList<KustoColumn> Columns);