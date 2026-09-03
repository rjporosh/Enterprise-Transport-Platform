namespace BookingService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Where the file-based diagnostic logs live (query logs, runtime-error
/// logs). Defaults to a path relative to the content root that resolves to
/// the repository <c>logs/</c> directory for <c>dotnet run</c> from
/// <c>src/BookingService.Api</c>. Override <c>Logging:FileLogsDirectory</c>
/// with an absolute path in any deployment where that relative assumption
/// does not hold. See <c>docs/programmers-guide/logging.md</c>.
/// </summary>
public sealed class FileLoggingOptions
{
    public const string SectionName = "Logging";

    /// <summary>
    /// Root of the file-log tree — sub-folders <c>query-logs/</c>,
    /// <c>runtime-errors/</c>, <c>build-errors/</c> are created under it.
    /// Default resolves to <c>services/booking-service/logs</c> from the API
    /// content root, matching bus-service / route-service.
    /// </summary>
    public string FileLogsDirectory { get; set; } = Path.Combine("..", "..", "logs");

    /// <summary>
    /// Query logging fires on every database round-trip — enabled by default
    /// in Development, opt-in elsewhere to avoid per-query I/O in production.
    /// </summary>
    public bool EnableQueryLogging { get; set; } = true;

    /// <summary>Queries slower than this are flagged with a "SLOW QUERY" hint and an optimisation suggestion in the log.</summary>
    public int SlowQueryThresholdMs { get; set; } = 300;
}
