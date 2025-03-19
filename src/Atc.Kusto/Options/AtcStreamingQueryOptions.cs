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
    /// Gets or sets a value indicating whether progressive mode is enabled.
    /// When enabled, results are streamed progressively as frames are received.
    /// </summary>
    public bool ProgressiveEnabled { get; set; } = true;
}