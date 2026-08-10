using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Flushes queued query-log entries to logs/query-log-&lt;dd-MM-yyyy&gt;.txt
/// every 2 seconds (one file per day, appended throughout — unlike the
/// per-incident build-error/runtime-error files). Reading straight from
/// this file after a slow endpoint report tells you exactly which query
/// ran, from where, and how long it took — see
/// docs/architecture/bus-service-architecture.md, "File-based diagnostic
/// logging".
/// </summary>
public sealed class QueryLogWriterBackgroundService : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    private readonly QueryLogSink _sink;
    private readonly IHostEnvironment _environment;
    private readonly FileLoggingOptions _options;
    private readonly ILogger<QueryLogWriterBackgroundService> _logger;

    public QueryLogWriterBackgroundService(QueryLogSink sink, IHostEnvironment environment, IOptions<FileLoggingOptions> options, ILogger<QueryLogWriterBackgroundService> logger)
    {
        _sink = sink;
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    private string LogsDirectory => Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.FileLogsDirectory));

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

        Flush(); // final drain on shutdown so nothing queued is lost
    }

    private void Flush()
    {
        var entries = _sink.DrainAll();
        if (entries.Count == 0) return;

        var fileName = $"query-log-{DateTime.UtcNow:dd-MM-yyyy}.txt";
        var path = Path.Combine(LogsDirectory, fileName);

        var lines = entries.Select(e =>
            $"[{e.StartedAtUtc:yyyy-MM-dd HH:mm:ss.fff} UTC] ({e.Duration.TotalMilliseconds:F1}ms) {e.Endpoint ?? "background"} :: {SingleLine(e.CommandText)}");

        try
        {
            File.AppendAllLines(path, lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write a batch of {Count} query-log entries to {Path}", entries.Count, path);
        }
    }

    private static string SingleLine(string sql) => sql.Replace("\r\n", " ").Replace("\n", " ").Trim();
}
