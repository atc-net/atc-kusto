namespace Atc.Kusto.HealthChecks;

/// <summary>
/// Represents the diagnostics information of a Kusto cluster.
/// </summary>
public class KustoClusterDiagnostics
{
    /// <summary>
    /// Gets or sets a value indicating whether the cluster is healthy.
    /// </summary>
    /// <value>
    /// <c>1</c> if the cluster is healthy; otherwise, <c>0</c>.
    /// </value>
    public int IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the reason why the cluster is not healthy.
    /// </summary>
    /// <value>
    /// The reason that the cluster is unhealthy. Only applicable when <see cref="IsHealthy"/> is <c>0</c>.
    /// </value>
    public string? NotHealthyReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cluster requires attention.
    /// </summary>
    /// <value>
    /// <c>1</c> if the cluster requires attention; otherwise, <c>0</c>.
    /// </value>
    public int IsAttentionRequired { get; set; }

    /// <summary>
    /// Gets or sets the reason why the cluster requires attention.
    /// </summary>
    /// <value>
    /// The reason that the cluster requires attention. Only applicable when <see cref="IsAttentionRequired"/> is <c>1</c>.
    /// </value>
    public string? AttentionRequiredReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether it is recommended to scale out the cluster.
    /// </summary>
    /// <value>
    /// <c>1</c> if scaling out is recommended; otherwise, <c>0</c>.
    /// </value>
    public int IsScaleOutRequired { get; set; }
}