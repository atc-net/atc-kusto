namespace Atc.Kusto.Analyzer.Parsing;

/// <summary>
/// Represents a parameter declared in a Kusto query.
/// </summary>
internal sealed class KustoParameter
{
    public KustoParameter(
        string name,
        string type,
        bool hasDefaultValue)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        HasDefaultValue = hasDefaultValue;
    }

    public string Name { get; }

    public string Type { get; }

    public bool HasDefaultValue { get; }
}