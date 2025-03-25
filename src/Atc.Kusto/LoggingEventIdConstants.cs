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
}