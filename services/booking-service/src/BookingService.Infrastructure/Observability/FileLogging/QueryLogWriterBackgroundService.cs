using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Flushes queued query-log entries to
/// <c>logs/query-logs/query-dd-MM-yyyy.txt</c> every 2 seconds (one file per
/// day, appended). Each entry is a structured block: provider + server,
/// service, endpoint, handler, generated SQL, started/finished/elapsed,
/// rows, parameters, correlation id, and — for slow queries — an
/// optimisation suggestion. See <c>docs/programmers-guide/logging.md</c>.
/// </summary>
public sealed class QueryLogWriterBackgroundService : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    private readonly QueryLogSink _sink;
    private readonly IHostEnvironment _environment;
    private readonly FileLoggingOptions _options;
    private readonly ILogger<QueryLogWriterBackgroundService> _logger;

    public QueryLogWriterBackgroundService(
        QueryLogSink sink,
        IHostEnvironment environment,
        IOptions<FileLoggingOptions> options,
        ILogger<QueryLogWriterBackgroundService> logger)
    {
        _sink = sink;
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    private string LogsDirectory =>
        Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.FileLogsDirectory, "query-logs"));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableQueryLogging) return;

        Directory.CreateDirectory(LogsDirectory);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(FlushInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            Flush();
        }

        Flush(); // final drain on shutdown
    }

    private void Flush()
    {
        var entries = _sink.DrainAll();
        if (entries.Count == 0) return;

        var path = Path.Combine(LogsDirectory, $"query-{DateTime.UtcNow:dd-MM-yyyy}.txt");
        var sb = new StringBuilder();

        foreach (var e in entries)
        {
            sb.AppendLine(new string('-', 78));
            sb.AppendLine($"Timestamp        : {e.StartedAtUtc:yyyy-MM-dd HH:mm:ss.fff} UTC");
            sb.AppendLine($"Database Provider : {e.DatabaseProvider}");
            sb.AppendLine($"Database Server   : {e.DatabaseServer}");
            sb.AppendLine($"Service           : {e.Service}");
            sb.AppendLine($"Endpoint          : {e.Endpoint ?? "(background job / startup)"}");
            sb.AppendLine($"Handler           : {e.Handler ?? "(n/a)"}");
            sb.AppendLine($"Correlation Id    : {e.CorrelationId ?? "(none)"}");
            sb.AppendLine($"Started At        : {e.StartedAtUtc:HH:mm:ss.fff} UTC");
            sb.AppendLine($"Finished At       : {e.FinishedAtUtc:HH:mm:ss.fff} UTC");
            sb.AppendLine($"Execution Time    : {e.Duration.TotalMilliseconds:F1} ms");
            sb.AppendLine($"Rows Affected     : {(e.RowsAffected.HasValue ? e.RowsAffected.Value.ToString() : "(read query — not reported)")}");
            sb.AppendLine($"Parameters        : {e.Parameters}");
            sb.AppendLine("Generated SQL     :");
            sb.AppendLine($"    {SingleLine(e.CommandText)}");
            if (e.Duration.TotalMilliseconds >= _options.SlowQueryThresholdMs)
            {
                sb.AppendLine($"SLOW QUERY (>= {_options.SlowQueryThresholdMs} ms)");
                sb.AppendLine($"Suggested Optimization : {Suggest(e.CommandText)}");
            }
        }

        try
        {
            File.AppendAllText(path, sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write {Count} query-log entries to {Path}", entries.Count, path);
        }
    }

    private static string SingleLine(string sql) => sql.Replace("\r\n", " ").Replace("\n", " ").Trim();

    private static string Suggest(string sql)
    {
        var lower = sql.ToLowerInvariant();
        if (lower.Contains(" like '%"))
            return "Leading-wildcard LIKE cannot use a b-tree index. Consider a trigram (pg_trgm) index or full-text search.";
        if (lower.Contains("order by") && !lower.Contains("limit"))
            return "ORDER BY without LIMIT sorts the whole result set. Add pagination (LIMIT/OFFSET or keyset).";
        if (lower.Contains("select") && lower.Contains(" join ") && lower.Contains("where") is false)
            return "Join without a WHERE filter — verify the query is not accidentally unbounded.";
        if (lower.Contains("count(*)"))
            return "COUNT(*) over a large table is slow on Postgres. Consider an approximate count or a maintained counter for hot paths.";
        return "Review the query plan with EXPLAIN (ANALYZE, BUFFERS); confirm every WHERE/JOIN column is covered by an index.";
    }
}
