namespace BookingService.Infrastructure.Observability.FileLogging;

/// <summary>
/// One executed database command, captured by <see cref="QueryLoggingInterceptor"/>
/// and rendered into <c>logs/query-logs/query-dd-MM-yyyy.txt</c> by
/// <see cref="QueryLogWriterBackgroundService"/> in the structured shape the
/// platform's diagnostic contract requires (see
/// <c>docs/programmers-guide/logging.md</c>).
/// </summary>
public sealed record QueryLogEntry(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    TimeSpan Duration,
    string DatabaseProvider,
    string DatabaseServer,
    string Service,
    string? Endpoint,
    string? Handler,
    string CommandText,
    string Parameters,
    int? RowsAffected,
    string? CorrelationId);
