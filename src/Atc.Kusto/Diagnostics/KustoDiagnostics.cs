namespace Atc.Kusto.Diagnostics;

/// <summary>
/// Provides OpenTelemetry diagnostics for Kusto operations.
/// <para>
/// To enable tracing, add the source to your OpenTelemetry configuration:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(tracing => tracing.AddSource(KustoDiagnostics.SourceName));
/// </code>
/// </para>
/// </summary>
public static class KustoDiagnostics
{
    /// <summary>
    /// The ActivitySource name for Atc.Kusto operations.
    /// Add this to your OpenTelemetry tracing configuration to enable Kusto telemetry.
    /// </summary>
    public const string SourceName = "Atc.Kusto";

    internal static readonly ActivitySource Source = new(SourceName, "1.0.0");

    internal static class ActivityNames
    {
        public const string Query = "kusto.query";
        public const string Command = "kusto.command";
        public const string StreamingQuery = "kusto.streaming";
    }

    internal static class TagNames
    {
        public const string DbStatement = "db.statement";
    }
}