using System.Text;

namespace BookingService.Api.Diagnostics;

/// <summary>
/// Writes <c>logs/runtime-errors/runtime-error-dd-MM-yyyy.txt</c> when the
/// service crashes or fails to start. Deliberately a plain static helper
/// with no DI dependency — the whole point is that it must still work when
/// the failure happened <em>before</em> the DI container finished building
/// (bad connection string, unreachable database at migration time, port in
/// use). Program.cs wraps the entire host startup/run in one try/catch that
/// calls this.
///
/// Includes a best-effort plain-English diagnosis + suggested fix for the
/// exception shapes most likely to mean "a dependency is down"
/// (Postgres/SqlServer/MySQL, Redis, RabbitMQ, port conflicts, missing
/// migrations) so "why won't it start" has one obvious file to read.
/// </summary>
public static class RuntimeErrorLogWriter
{
    public static string Write(Exception exception, string contentRootPath, string environmentName, string relativeLogsDirectory = "../../logs")
    {
        var logsDirectory = Path.GetFullPath(Path.Combine(contentRootPath, relativeLogsDirectory, "runtime-errors"));
        Directory.CreateDirectory(logsDirectory);

        var path = Path.Combine(logsDirectory, $"runtime-error-{DateTime.UtcNow:dd-MM-yyyy}.txt");

        var (cause, solution) = Diagnose(exception);

        var content = new StringBuilder();
        content.AppendLine(new string('-', 78));
        content.AppendLine("RUNTIME ERROR — booking-service crashed or failed to start");
        content.AppendLine($"Timestamp    : {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
        content.AppendLine($"Service      : booking-service");
        content.AppendLine($"Environment  : {environmentName}");
        content.AppendLine($"Content root : {contentRootPath}");
        content.AppendLine($"Root cause   : {cause}");
        content.AppendLine($"Possible fix : {solution}");
        content.AppendLine(new string('-', 78));
        content.AppendLine("FULL EXCEPTION CHAIN:");

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
            File.AppendAllText(path, content.ToString());
        }
        catch
        {
            // Writing the diagnostic file must never be what crashes the crash handler.
        }

        return path;
    }

    private static (string Cause, string Solution) Diagnose(Exception exception)
    {
        var text = $"{exception.GetType().FullName} {exception.Message} {exception.InnerException?.Message}".ToLowerInvariant();

        if (text.Contains("npgsql") || text.Contains("28p01") || (text.Contains("postgres") && text.Contains("connect")))
            return ("PostgreSQL is unreachable or rejected the connection (booking_service database).",
                "Start Postgres (docker compose up -d postgres) and verify ConnectionStrings:BookingDb in appsettings.json. From the repo root: docker compose -f infrastructure/docker/docker-compose.yml up -d postgres.");

        if (text.Contains("relation") && text.Contains("does not exist") || text.Contains("pending model changes") || text.Contains("no migrations"))
            return ("The booking_service schema has not been created — migrations are missing or unapplied.",
                "Run: dotnet ef database update --project services/booking-service/src/BookingService.Infrastructure --startup-project services/booking-service/src/BookingService.Api. See MIGRATIONS.md.");

        if (text.Contains("sqlclient") || text.Contains("sql server"))
            return ("SQL Server is unreachable or rejected the connection.", "Verify Database:Provider=SqlServer and the connection string; ensure SQL Server is running.");

        if (text.Contains("mysql") || text.Contains("mysqlconnector"))
            return ("MySQL is unreachable or rejected the connection.", "Verify Database:Provider=MySql and the connection string; ensure MySQL is running.");

        if (text.Contains("redis") || text.Contains("stackexchange.redis"))
            return ("Redis is unreachable at startup (the ConnectionMultiplexer.Connect call).",
                "Start Redis (docker compose up -d redis) or set Redis:ConnectionString. Booking normally fails open on Redis, but the initial connect still needs it reachable or AbortOnConnectFail=false.");

        if (text.Contains("rabbitmq") || text.Contains("amqp") || text.Contains("brokerunreachable"))
            return ("RabbitMQ is unreachable — the outbox relay and event consumers cannot connect.",
                "Start RabbitMQ (docker compose up -d rabbitmq) and verify RabbitMq:HostName/Port/UserName/Password. Booking degrades gracefully: the API still serves reads/writes; events queue in the outbox until the broker returns.");

        if (text.Contains("address already in use") || text.Contains("eaddrinuse"))
            return ("The port booking-service binds to is already in use.", "Stop the other process or pass --urls http://localhost:<free-port>.");

        return ("Unclassified startup failure.", "Read the full exception chain below; check appsettings.json and that every dependency (Postgres, RabbitMQ, Redis) is running.");
    }
}
