namespace Atc.Kusto;

/// <summary>
/// Represents an abstract base class for Kusto commands, providing the structure for executing commands against a Kusto database.
/// </summary>
public abstract record KustoCommand : KustoScript, IKustoCommand;