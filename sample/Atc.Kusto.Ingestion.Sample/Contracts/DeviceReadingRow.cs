namespace Atc.Kusto.Ingestion.Sample.Contracts;

/// <summary>
/// A row read back from the table. Every column is nullable so the mapping-mismatch scenario can
/// show the empty columns it produces.
/// </summary>
public sealed record DeviceReadingRow(
    string? DeviceId,
    string? SerialNumber,
    double? Value,
    DateTime? Timestamp);