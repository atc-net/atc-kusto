namespace Atc.Kusto.Options;

/// <summary>
/// Provides configuration options for executing a streaming query,
/// including which frame headers to include in the response and whether to enable progressive mode.
/// </summary>
public sealed class AtcStreamingQueryOptions : AtcQueryOptionsBase
{
    /// <summary>
    /// Gets or sets the optional frame headers to include in the response.
    /// <para>
    /// Default is <see cref="FrameHeaders.All"/>.
    /// </para>
    /// <para>
    /// Available optional FrameHeaders options:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="FrameHeaders.None"/>: No frame headers are included.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="FrameHeaders.DataSetHeader"/>: Includes the data set header frame.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="FrameHeaders.TableHeader"/>: Includes the table header frame.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="FrameHeaders.CompletionSummary"/>: Includes the completion summary frame.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="FrameHeaders.All"/>: A combination of DataSetHeader, TableHeader, and CompletionSummary.</description>
    ///   </item>
    /// </list>
    /// </summary>
    public FrameHeaders OptionalFrames { get; set; } = FrameHeaders.None;

    /// <summary>
    /// Gets or sets a value indicating whether Kusto sends the result in progressive mode.
    /// Default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are streamed one by one in both modes. In progressive mode the server may also send a preview of the
    /// result while it is still computing, and later replace it (e.g. for <c>summarize</c>, where an early count is
    /// replaced by the final one).
    /// </para>
    /// <para>
    /// Only enable it for long-running queries where an early preview is worth it, preferably with
    /// <see cref="IKustoProcessor.ExecuteBufferedStreamingQuery{T}(IKustoStreamingQuery{T}, AtcStreamingQueryOptions?, CancellationToken)"/>,
    /// which applies a replace before handing out rows. Direct streaming hands rows out immediately and can't take
    /// them back, so a replace after rows were returned ends the stream with an <see cref="InvalidOperationException"/>.
    /// Plain <c>take</c>/<c>where</c>/<c>project</c> queries don't get their rows any sooner.
    /// </para>
    /// </remarks>
    public bool ProgressiveEnabled { get; set; }
}