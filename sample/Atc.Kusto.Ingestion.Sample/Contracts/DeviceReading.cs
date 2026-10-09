namespace Atc.Kusto.Ingestion.Sample.Contracts;

/// <summary>
/// A row to ingest. Serialized with camelCase names by default (<c>"deviceId"</c>, <c>"serialNumber"</c>, …),
/// matching the <c>$.camelCase</c> paths of the mapping in <c>setup.kql</c>.
/// </summary>
public sealed record DeviceReading(
    string RunId,
    string Scenario,
    string DeviceId,
    string SerialNumber,
    double Value,
    DateTimeOffset Timestamp);