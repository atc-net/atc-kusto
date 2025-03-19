namespace Atc.Kusto.Sample.Contracts;

public record StormEvent(
    DateTimeOffset StartTime,
    string EventType,
    string State);