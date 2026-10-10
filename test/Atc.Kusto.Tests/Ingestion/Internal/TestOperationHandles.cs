namespace Atc.Kusto.Tests.Ingestion.Internal;

/// <summary>
/// Creates real V2 operation handles for tests. <see cref="IngestionOperation"/> has no public
/// constructor, so the SDK's internal one is invoked via reflection.
/// </summary>
internal static class TestOperationHandles
{
    public static IngestionOperation CreateOperation(
        IngestionMethod method,
        string operationId = "op-123")
    {
        var constructor = typeof(IngestionOperation)
            .GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Single(c => c.GetParameters().Length == 5);

        return (IngestionOperation)constructor.Invoke(
            [
                "Db",
                "Events",
                method,
                operationId,
                Array.Empty<IngestResult>(),
            ]);
    }

    public static string CreateHandle(
        IngestionMethod method,
        string operationId = "op-123")
        => KustoIngestClient.SerializeOperation(CreateOperation(method, operationId));
}