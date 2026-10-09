namespace Atc.Kusto.Api.Sample.Contracts;

/// <summary>
/// A row in the <c>SampleDeviceReadings</c> table created by
/// <c>sample/Atc.Kusto.Ingestion.Sample/setup.kql</c>. Written as camelCase JSON, matching the
/// table's <c>SampleDeviceReadings_mapping</c>.
/// </summary>
public record IngestedDeviceReading(
    string RunId,
    string Scenario,
    string DeviceId,
    string SerialNumber,
    double Value,
    DateTimeOffset Timestamp);