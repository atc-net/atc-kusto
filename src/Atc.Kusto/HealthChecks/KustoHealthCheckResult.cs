namespace Atc.Kusto.HealthChecks;

/// <summary>
/// Represents the result of a Kusto cluster health check.
/// </summary>
public class KustoHealthCheckResult
{
    /// <summary>
    /// Gets a value indicating whether the cluster is healthy.
    /// </summary>
    /// <value>
    /// <c>true</c> if the cluster is healthy; otherwise, <c>false</c>.
    /// </value>
    public bool IsHealthy { get; }

    /// <summary>
    /// Gets the reason why the cluster is not healthy, if applicable.
    /// </summary>
    /// <value>
    /// A string describing why the cluster is not healthy, or <c>null</c> if the cluster is healthy.
    /// </value>
    public string? NotHealthyReason { get; }

    /// <summary>
    /// Gets a value indicating whether the cluster requires attention.
    /// </summary>
    /// <value>
    /// <c>true</c> if the cluster requires attention; otherwise, <c>false</c>.
    /// </value>
    public bool IsAttentionRequired { get; }

    /// <summary>
    /// Gets the reason why the cluster requires attention, if applicable.
    /// </summary>
    /// <value>
    /// A string describing why the cluster requires attention, or <c>null</c> if the cluster does not require attention.
    /// </value>
    public string? AttentionRequiredReason { get; }

    /// <summary>
    /// Gets a value indicating whether it is recommended to scale out the cluster.
    /// </summary>
    /// <value>
    /// <c>true</c> if scaling out is recommended; otherwise, <c>false</c>.
    /// </value>
    public bool IsScaleOutRequired { get; }

    /// <summary>
    /// Gets the duration of the health check operation.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KustoHealthCheckResult"/> class.
    /// </summary>
    /// <param name="isHealthy">Indicates whether the cluster is healthy.</param>
    /// <param name="notHealthyReason">The reason why the cluster is not healthy, if applicable.</param>
    /// <param name="isAttentionRequired">Indicates whether the cluster requires attention.</param>
    /// <param name="attentionRequiredReason">The reason why the cluster requires attention, if applicable.</param>
    /// <param name="isScaleOutRequired">Indicates whether it is recommended to scale out the cluster.</param>
    /// <param name="duration">The duration of the health check operation.</param>
    public KustoHealthCheckResult(
        bool isHealthy,
        string? notHealthyReason,
        bool isAttentionRequired,
        string? attentionRequiredReason,
        bool isScaleOutRequired,
        TimeSpan duration)
    {
        IsHealthy = isHealthy;
        NotHealthyReason = notHealthyReason;
        IsAttentionRequired = isAttentionRequired;
        AttentionRequiredReason = attentionRequiredReason;
        IsScaleOutRequired = isScaleOutRequired;
        Duration = duration;
    }
}