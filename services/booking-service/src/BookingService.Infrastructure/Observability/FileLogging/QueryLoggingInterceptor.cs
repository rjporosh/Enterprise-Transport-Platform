using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BookingService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Captures every SQL statement EF Core executes — text, parameters, start
/// time, duration, rows — and hands it to <see cref="IQueryLogSink"/> for the
/// background writer to persist to <c>logs/query-logs/</c>. Registered
/// per-DbContext via <c>AddInterceptors()</c>, only when
/// <c>Logging:EnableQueryLogging</c> is true.
///
/// Only the *Async overrides are implemented — this codebase exclusively
/// uses EF Core's async query methods, so the sync interception points would
/// be dead code.
/// </summary>
public sealed class QueryLoggingInterceptor : DbCommandInterceptor
{
    private const string Service = "booking-service";

    private readonly IQueryLogSink _sink;
    private readonly string _provider;
    private readonly string _server;

    public QueryLoggingInterceptor(IQueryLogSink sink, string provider, string server)
    {
        _sink = sink;
        _provider = provider;
        _server = server;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData, rows: null);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData, rows: result);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData, rows: null);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void Log(DbCommand command, CommandExecutedEventData eventData, int? rows)
    {
        var started = eventData.StartTime;
        var entry = new QueryLogEntry(
            StartedAtUtc: started,
            FinishedAtUtc: started.Add(eventData.Duration),
            Duration: eventData.Duration,
            DatabaseProvider: _provider,
            DatabaseServer: _server,
            Service: Service,
            Endpoint: CurrentRequestContext.Endpoint,
            Handler: CurrentRequestContext.Handler,
            CommandText: command.CommandText,
            Parameters: FormatParameters(command),
            RowsAffected: rows,
            CorrelationId: CurrentRequestContext.CorrelationId);

        _sink.Enqueue(entry);
    }

    private static string FormatParameters(DbCommand command)
    {
        if (command.Parameters.Count == 0) return "(none)";

        var sb = new StringBuilder();
        for (var i = 0; i < command.Parameters.Count; i++)
        {
            var p = command.Parameters[i];
            if (i > 0) sb.Append(", ");
            sb.Append(p.ParameterName).Append('=').Append(p.Value ?? "NULL");
        }
        return sb.ToString();
    }
}
