namespace Atc.Kusto.HealthChecks;

/// <summary>
/// Represents a Kusto query for checking cluster health.
/// </summary>
public record KustoHealthCheckQuery
    : KustoQuery<KustoClusterDiagnostics>;