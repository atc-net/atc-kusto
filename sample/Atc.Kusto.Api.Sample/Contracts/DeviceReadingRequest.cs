namespace Atc.Kusto.Api.Sample.Contracts;

public record DeviceReadingRequest(
    string DeviceId,
    string SerialNumber,
    double Value,
    DateTimeOffset Timestamp);