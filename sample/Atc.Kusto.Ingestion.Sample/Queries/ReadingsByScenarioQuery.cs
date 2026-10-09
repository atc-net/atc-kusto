namespace Atc.Kusto.Ingestion.Sample.Queries;

public sealed record ReadingsByScenarioQuery(
    string RunId,
    string Scenario)
    : KustoQuery<DeviceReadingRow>;