using System.Text;

namespace BusService.Api.Diagnostics;

/// <summary>
/// Writes logs/runtime-error-&lt;dd-MM-yyyy-HH-mm-ss&gt;.txt when the service
/// crashes or fails to start — deliberately a plain static helper with no
/// DI dependency, because the whole point is that it must still work when
/// the crash happened *before* the DI container finished building (a bad
/// connection string, an unreachable database at migration time, a port
/// already in use). See Program.cs, where this wraps the entire host
/// startup/run in one try/catch.
///
/// Includes a best-effort plain-English diagnosis for the exception types
/// most likely to mean "a dependency is down" (Postgres/SqlServer/MySQL,
/// Redis, RabbitMQ) — the whole reason this exists is so "why won't it
/// start" has one obvious file to check instead of scrolling a terminal.
/// </summary>
public static class RuntimeErrorLogWriter
{
    public static string Write(Exception exception, string contentRootPath, string relativeLogsDirectory = "../../logs")
    {
        var logsDirectory = Path.GetFullPath(Path.Combine(contentRootPath, relativeLogsDirectory));
        Directory.CreateDirectory(logsDirectory);

        var fileName = $"runtime-error-{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss}.txt";
        var path = Path.Combine(logsDirectory, fileName);

        var content = new StringBuilder();
        content.AppendLine("RUNTIME ERROR — service crashed or failed to start");
        content.AppendLine($"Timestamp:   {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} (UTC)");
        content.AppendLine($"Content root: {contentRootPath}");
        content.AppendLine(new string('-', 72));

        var diagnosis = Diagnose(exception);
        if (diagnosis is not null)
        {
            content.AppendLine("LIKELY CAUSE:");
            content.AppendLine(diagnosis);
            content.AppendLine(new string('-', 72));
        }

        content.AppendLine("FULL EXCEPTION DETAILS:");
        var current = exception;
        var depth = 0;
        while (current is not null)
        {
            content.AppendLine(depth == 0 ? "Exception:" : $"Inner exception (depth {depth}):");
            content.AppendLine($"  Type:    {current.GetType().FullName}");
            content.AppendLine($"  Message: {current.Message}");
            content.AppendLine("  Stack trace:");
            content.AppendLine(current.StackTrace ?? "  (none)");
            content.AppendLine();
            current = current.InnerException;
            depth++;
        }

        try
        {
            File.WriteAllText(path, content.ToString());
        }
        catch
        {
            // Writing the diagnostic file itself must never be what crashes
            // the crash handler — if this fails (read-only filesystem, disk
            // full), the original exception is still thrown/logged normally
            // by the caller; this is a best-effort convenience, not the
            // only record of the failure.
        }

        return path;
    }

    private static string? Diagnose(Exception exception)
    {
        var fullText = $"{exception.GetType().FullName} {exception.Message} {exception.InnerException?.Message}".ToLowerInvariant();

        if (fullText.Contains("npgsql") || fullText.Contains("28p01") || (fullText.Contains("postgres") && fullText.Contains("connect")))
            return "PostgreSQL appears to be unreachable or rejected the connection. Check that Postgres is running and that ConnectionStrings:BusDb in appsettings.json (or the Database:Provider-matching connection string) is correct.";

        if (fullText.Contains("sqlclient") || fullText.Contains("sql server"))
            return "SQL Server appears to be unreachable or rejected the connection. Check that SQL Server is running and the connection string is correct.";

        if (fullText.Contains("mysql") || fullText.Contains("mysqlconnector"))
            return "MySQL appears to be unreachable or rejected the connection. Check that MySQL is running and the connection string is correct.";

        if (fullText.Contains("redis") || fullText.Contains("stackexchange.redis"))
            return "Redis appears to be unreachable. Check that Redis is running and Redis:ConnectionString in appsettings.json points at it. Note: Redis is normally configured to fail open (see RedisCacheService) — this diagnosis fired because something bypassed that, most likely the initial ConnectionMultiplexer.Connect() call at startup.";

        if (fullText.Contains("rabbitmq") || fullText.Contains("amqp") || fullText.Contains("brokerunreachable"))
            return "RabbitMQ appears to be unreachable. Check that RabbitMQ is running and RabbitMq:HostName/Port/UserName/Password in appsettings.json are correct.";

        if (fullText.Contains("address already in use") || fullText.Contains("can't assign requested address") || fullText.Contains("eaddrinuse"))
            return "The port this service is trying to bind to is already in use (or --urls was malformed — remember to include the host, e.g. http://localhost:5xxx, not just the port). Check for another process already listening on that port, or pass a different --urls value.";

        if (fullText.Contains("requires a primary key") || fullText.Contains("no primary key"))
            return "An EF Core entity is missing its key mapping, or a property EF Core auto-discovered as a navigation (e.g. a public collection property that should have been Ignore()d) has no key. Check the relevant EntityTypeConfiguration.";

        if (fullText.Contains("pending model changes") || fullText.Contains("migrations"))
            return "The database schema does not match the current EF Core model, or no migrations have been applied yet. Run the pending migration(s) — see the service README, \"Running locally\".";

        return null;
    }
}
