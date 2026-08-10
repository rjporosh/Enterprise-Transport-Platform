namespace BusService.Infrastructure.Observability.FileLogging;

public sealed record QueryLogEntry(DateTimeOffset StartedAtUtc, TimeSpan Duration, string? Endpoint, string CommandText);
