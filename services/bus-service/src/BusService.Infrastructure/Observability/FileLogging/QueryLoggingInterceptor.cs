using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BusService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Captures every SQL statement EF Core actually executes — text, start
/// time, and duration — and hands it to <see cref="IQueryLogSink"/> for the
/// background writer to persist. Registered per-DbContext via
/// AddInterceptors() in DependencyInjection.cs, only when
/// Logging:EnableQueryLogging is true.
///
/// Only the *Async overrides are implemented deliberately — this codebase
/// exclusively uses EF Core's async query methods (ToListAsync,
/// FirstOrDefaultAsync, SaveChangesAsync, etc.), so the sync interception
/// points would never fire; adding them would be dead code pretending to be
/// coverage. If a future change introduces a synchronous EF Core call, add
/// the matching sync override then.
/// </summary>
public sealed class QueryLoggingInterceptor : DbCommandInterceptor
{
    private readonly IQueryLogSink _sink;

    public QueryLoggingInterceptor(IQueryLogSink sink) => _sink = sink;

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void Log(DbCommand command, CommandExecutedEventData eventData)
    {
        var entry = new QueryLogEntry(eventData.StartTime, eventData.Duration, CurrentRequestContext.Endpoint, command.CommandText);
        _sink.Enqueue(entry);
    }
}
