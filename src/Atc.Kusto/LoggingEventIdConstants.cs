namespace Atc.Kusto;

public static class LoggingEventIdConstants
{
    internal static class SimpleQueryHandler
    {
        public const int KustoServicePartialQueryFailureException = 10_000;
        public const int KustoServiceException = 10_010;
        public const int SemanticException = 10_020;
        public const int UnhandledException = 10_030;
    }

    internal static class ExistingPagedStoredQueryHandler
    {
        public const int KustoServicePartialQueryFailureException = 20_000;
        public const int KustoServiceException = 20_010;
        public const int SemanticException = 20_020;
        public const int UnhandledException = 20_030;
    }

    internal static class ResiliencePipeline
    {
        public const int Retry = 30_000;
    }

    internal static class BufferedStreamingQueryHandler
    {
        public const int UnhandledException = 40_000;
        public const int SchemaNullInDataTableDataSetFrame = 40_010;
        public const int ReceivedTableFragmentForUnknownTable = 40_020;
        public const int ProgressiveDataSetIsNull = 40_030;
    }

    internal static class StreamingQueryHandler
    {
        public const int SchemaNullInDataTableDataSetFrame = 50_000;
        public const int ReceivedTableFragmentForUnknownTable = 50_010;
    }

    internal static class KustoClusterDiagnosticsHealthCheck
    {
        public const int NoDiagnosticDataReturned = 60_000;
        public const int ClusterRequiresAttention = 60_010;
        public const int ClusterScaleOutRecommended = 60_020;
        public const int HealthCheckFailed = 60_030;
        public const int HealthCheckSucceeded = 60_040;
        public const int UnhandledException = 60_050;
    }

    internal static class CancellationTokenKustoExtensions
    {
        public const int KustoCancelCommandFailed = 70_000;
        public const int FailedToScheduleKustoCancel = 70_010;
    }
}