namespace Atc.Kusto.Options;

public class AtcKustoOptions
{
    public Uri? HostAddress { get; set; }

    public string? DatabaseName { get; set; }

    public TokenCredential? Credential { get; set; }

    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the ingestion mode used when a request does not specify one.
    /// </summary>
    public IngestionMode DefaultIngestionMode { get; set; } = IngestionMode.ManagedStreaming;

    /// <summary>
    /// Gets the blob containers that queued and managed-streaming ingestion uploads to.
    /// </summary>
    /// <remarks>
    /// Supply your own, privately reachable containers when the cluster sits behind a private
    /// endpoint. When empty, the SDK's default Kusto-internal storage is used. The connection's
    /// credential is reused for these containers, so that identity needs Storage Blob Data
    /// Contributor on them.
    /// </remarks>
    public IList<Uri> IngestUploadContainers { get; } = new List<Uri>();

    /// <summary>
    /// Gets or sets how long Kusto keeps the stored query result behind a paged query.
    /// Default is 1 hour.
    /// </summary>
    /// <remarks>
    /// Must be between 1 second and 24 hours; whole seconds are used. A later page that arrives after the
    /// result has expired runs the query again, so a longer time means fewer re-runs, while Kusto keeps
    /// each result for longer.
    /// </remarks>
    public TimeSpan PagedResultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the JSON serializer settings of this connection, used to map query results to objects and to write
    /// ingested rows.
    /// </summary>
    /// <remarks>
    /// The defaults write camelCase property names, read property names case-insensitively, read numbers from
    /// strings, write enums as strings, and handle Kusto's <c>bool</c> and <see cref="DateOnly"/> values.
    /// Change them with <see cref="ConfigureSerializerOptions"/> when the connection is registered:
    /// System.Text.Json doesn't allow changes once the settings have been used.
    /// </remarks>
    public JsonSerializerOptions SerializerOptions { get; internal set; } = KustoJsonSerializerOptions.CreateDefault();

    /// <summary>
    /// Changes the JSON serializer settings of this connection, starting from the defaults.
    /// </summary>
    /// <param name="configure">Changes the settings, e.g. <c>json => json.PropertyNamingPolicy = null</c>.</param>
    /// <returns>The same options, so calls can be chained.</returns>
    public AtcKustoOptions ConfigureSerializerOptions(
        Action<JsonSerializerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(SerializerOptions);
        return this;
    }
}