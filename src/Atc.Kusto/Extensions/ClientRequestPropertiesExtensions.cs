namespace Atc.Kusto.Extensions;

/// <summary>
/// Extension helpers for <see cref="ClientRequestProperties"/> that apply common
/// <c>.set option</c> values based on <see cref="AtcQueryOptionsBase"/> implementations.
/// </summary>
public static class ClientRequestPropertiesExtensions
{
    /// <summary>
    /// Applies <see cref="AtcStreamingQueryOptions"/> to the supplied <paramref name="clientRequestProperties"/>,
    /// including progressive‑results settings.
    /// </summary>
    /// <param name="clientRequestProperties">Target request‑properties instance.</param>
    /// <param name="streamingQueryOptions">Options to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null"/>.</exception>
    public static void SetQueryOptions(
        this ClientRequestProperties clientRequestProperties,
        AtcStreamingQueryOptions streamingQueryOptions)
    {
        ArgumentNullException.ThrowIfNull(clientRequestProperties);
        ArgumentNullException.ThrowIfNull(streamingQueryOptions);

        clientRequestProperties.SetBaseQueryOptions(streamingQueryOptions);

        if (streamingQueryOptions.ProgressiveEnabled)
        {
            clientRequestProperties.SetOption(ClientRequestProperties.OptionResultsProgressiveEnabled, value: true);
        }
    }

    /// <summary>
    /// Applies <see cref="AtcQueryOptions"/> to the supplied <paramref name="clientRequestProperties"/>.
    /// </summary>
    /// <param name="clientRequestProperties">Target request‑properties instance.</param>
    /// <param name="queryOptions">Options to apply.</param>
    public static void SetQueryOptions(
        this ClientRequestProperties clientRequestProperties,
        AtcQueryOptions queryOptions)
        => clientRequestProperties.SetBaseQueryOptions(queryOptions);

    /// <summary>
    /// Core helper that maps values from <see cref="AtcQueryOptionsBase"/> into the corresponding
    /// Kusto request‑property options.
    /// </summary>
    /// <param name="clientRequestProperties">Target request‑properties instance.</param>
    /// <param name="queryOptions">Options to map.</param>
    private static void SetBaseQueryOptions(
        this ClientRequestProperties clientRequestProperties,
        AtcQueryOptionsBase queryOptions)
    {
        ArgumentNullException.ThrowIfNull(clientRequestProperties);
        ArgumentNullException.ThrowIfNull(queryOptions);

        if (queryOptions.QueryTakeMaxRecords is not null && queryOptions.QueryTakeMaxRecords.Value > 0)
        {
            clientRequestProperties.SetOption(ClientRequestProperties.OptionTakeMaxRecords, value: queryOptions.QueryTakeMaxRecords.Value);
        }

        if (queryOptions.NoTruncation)
        {
            clientRequestProperties.SetOption(ClientRequestProperties.OptionNoTruncation, value: true);
        }

        if (queryOptions.TruncationMaxRecords is not null && queryOptions.TruncationMaxRecords.Value > 0)
        {
            clientRequestProperties.SetOption(ClientRequestProperties.OptionTruncationMaxRecords, value: queryOptions.TruncationMaxRecords.Value);
        }

        if (queryOptions.TruncationMaxSize is not null && queryOptions.TruncationMaxSize.Value > 0)
        {
            clientRequestProperties.SetOption(ClientRequestProperties.OptionTruncationMaxSize, value: queryOptions.TruncationMaxSize.Value);
        }
    }
}