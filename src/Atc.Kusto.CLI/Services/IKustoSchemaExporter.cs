namespace Atc.Kusto.CLI.Services;

/// <summary>
/// Exports Azure Data Explorer (Kusto) database schema artifacts to KQL script files.
/// </summary>
public interface IKustoSchemaExporter
{
    /// <summary>
    /// Exports all table definitions from the specified database as KQL scripts.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <param name="outputDir">The root output directory for exported files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExportTablesAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all function definitions from the specified database as KQL scripts.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <param name="outputDir">The root output directory for exported files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExportFunctionsAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all materialized view definitions from the specified database as KQL scripts.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <param name="outputDir">The root output directory for exported files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExportMaterializedViewsAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all external table definitions from the specified database as KQL scripts.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <param name="outputDir">The root output directory for exported files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExportExternalTablesAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports retention and caching policies for the database and its tables as KQL scripts.
    /// </summary>
    /// <param name="tenantId">The Azure AD tenant identifier.</param>
    /// <param name="clusterUrl">The Kusto cluster URL.</param>
    /// <param name="database">The database name.</param>
    /// <param name="outputDir">The root output directory for exported files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExportPoliciesAsync(
        string tenantId,
        Uri clusterUrl,
        string database,
        string outputDir,
        CancellationToken cancellationToken = default);
}