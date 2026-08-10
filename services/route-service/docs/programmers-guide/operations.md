# Route Service - Operations Guide

## Logging

### Structured Logs (Serilog)

All logs are structured JSON to stdout. In Docker/Kubernetes, these are
collected by the sidecar or log shipper.

Log levels:
- `Information` — default
- `Warning` — retries, circuit breaks (future)
- `Error` — unhandled exceptions, save failures

### File-Based Diagnostics

| File | When Written | Contents |
|------|-------------|---------|
| `logs/runtime-error-<dd-MM-yyyy>.txt` | Startup or crash | Exception type, message, stack trace, plain-English diagnosis |
| `logs/query-log-<dd-MM-yyyy>.txt` | Per-query (opt-in) | SQL text, duration, triggering endpoint |

Enable query logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "EnableQueryLogging": true
  }
}
```

### External Aggregation

| Stack | Integration |
|-------|-------------|
| **Grafana Loki** | Ship stdout JSON via Promtail; query in Grafana Explore |
| **Elasticsearch + Kibana** | Ship via Filebeat or Fluentd; use the provided index pattern `route-service-*` |
| **Graylog** | Ship via Sidecar/GELF; input type `GELF TCP` |
| **OpenTelemetry Collector** | Logs are already emitted as OTLP logs alongside traces; route to any backend |

## Metrics

Prometheus metrics at `/metrics`:
- `route_service_routes_created_total`
- `route_service_schedules_activated_total`
- `http_server_duration_seconds` (AspNetCore)
- `dotnet_runtime_*` (runtime)

## Tracing

OpenTelemetry traces exported to `OpenTelemetry:OtlpEndpoint` (default
`http://localhost:4317`). Spans cover:
- Incoming HTTP requests
- EF Core queries
- Outgoing HTTP calls (future)
- gRPC calls

## Health Checks

| Endpoint | Purpose |
|----------|---------|
| `/health` | Combined liveness + readiness |
| `/health/live` | Liveness only |
| `/health/ready` | Readiness (DB, Redis, RabbitMQ) |

## CronJobs / Background Services

### Built-in

- **OutboxProcessor** — polls `outbox_messages` every 5 seconds, publishes
  unprocessed events to RabbitMQ, marks them `ProcessedOnUtc`.

### Adding a Scheduled Job

Use Quartz.NET for cron-style scheduling:

```csharp
// Features/Schedules/ActivateDueSchedulesJob.cs
public sealed class ActivateDueSchedulesJob : IJob
{
    private readonly IRouteDbContext _context;
    private readonly IDateTimeProvider _clock;

    public ActivateDueSchedulesJob(IRouteDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var due = await _context.Schedules
            .Where(s => !s.IsDeleted && s.Status == ScheduleStatus.Planned && s.EffectiveFrom <= _clock.UtcNow)
            .ToListAsync(context.CancellationToken);

        foreach (var s in due) s.Activate(_clock.UtcNow);
        await _context.SaveChangesAsync(context.CancellationToken);
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    var jobKey = new JobKey("ActivateDueSchedules");
    q.AddJob<ActivateDueSchedulesJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ActivateDueSchedules-trigger")
        .WithCronSchedule("0 *\/1 * * * ?")); // every minute
});
builder.Services.AddQuartzHostedService();
```

## Load / Stress Testing

Scripts belong in `tests/load/`. Run:

```bash
# k6
k6 run tests/load/k6/route-load-test.js

# NBomber
dotnet run --project tests/load/NBomber/NBomber.LoadTests.csproj

# JMeter (GUI or CLI)
jmeter -n -t tests/load/jmeter/route-test-plan.jmx -l results.jtl
```

Target endpoints: `GET /api/v1/routes`, `POST /api/v1/routes`, `GET /api/v1/stops`.

## Database Maintenance

### Vacuum / Analyze (Postgres)

```sql
VACUUM ANALYZE route.routes;
VACUUM ANALYZE route.stops;
VACUUM ANALYZE route.route_stops;
VACUUM ANALYZE route.schedules;
```

### Index Maintenance

```sql
REINDEX INDEX route.IX_routes_Code;
REINDEX INDEX route.IX_stops_Code;
```

### Archiving Soft-Deleted Rows

Soft-deleted rows stay in place. To archive:

```sql
CREATE TABLE route.routes_archive AS
SELECT * FROM route.routes WHERE IsDeleted = true;
DELETE FROM route.routes WHERE IsDeleted = true;
```

## Backup / Restore

```bash
# Postgres
pg_dump -U postgres -d route_service -n route -F c -f route_service_backup.dump
pg_restore -U postgres -d route_service_restored -F c route_service_backup.dump

# MySQL
mysqldump -u root -p route_service route > route_service.sql

# SQL Server
sqlcmd -S localhost -U sa -P password -Q "BACKUP DATABASE route_service TO DISK = 'route_service.bak'"
```

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| `HostAbortedException` on startup | gRPC services not registered | Ensure `builder.Services.AddGrpc()` is called |
| Empty EF migration | Stop query filter conflict | Remove `HasQueryFilter` from `StopConfiguration` |
| `Unable to resolve DbContextOptions` | Design-time factory missing | Ensure `RouteDbContext` has a public constructor with `DbContextOptions` |
| 401 on all endpoints | JWT signing key mismatch | Verify `Jwt:SigningKey` matches Auth Service |
| Slow `/api/v1/routes` | Missing index on `Status` | Add index or enable query logging to inspect plan |
