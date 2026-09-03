# Logging

Two complementary layers:

1. **Structured app logs** — Serilog → console + Seq (`Seq:ServerUrl`) +
   OpenTelemetry. Query these in Seq / Grafana / Jaeger. This is the primary
   channel.
2. **File-based diagnostic logs** — plain-text, one folder per category under
   `services/<svc>/logs/`, for the "one obvious file to read when X breaks"
   cases. Git-ignored (`**/logs/`). Present in booking-service, bus-service,
   route-service today; rolled out per service as each is touched.

## `logs/runtime-errors/runtime-error-dd-MM-yyyy.txt`

Written when a service **crashes or fails to start**, and for every `5xx` the
API returns. Each block: timestamp, service, environment, **diagnosed root
cause**, **suggested fix**, then the full exception chain. The diagnosis
recognises the "a dependency is down" shapes — Postgres / SQL Server / MySQL
unreachable, missing migration, Redis, RabbitMQ, port-in-use — so startup
failures are self-explaining.

Producer: `src/<Svc>.Api/Diagnostics/RuntimeErrorLogWriter.cs` (no DI — must work
before the container is built), called from `Program.cs`'s top-level
try/catch and from `ExceptionHandlingMiddleware` for 5xx.

## `logs/query-logs/query-dd-MM-yyyy.txt`

Every SQL statement EF Core executes, appended (one file/day). Each block:
`Timestamp, Database Provider, Database Server, Service, Endpoint, Handler,
Correlation Id, Started At, Finished At, Execution Time, Rows Affected,
Parameters, Generated SQL` — plus, for statements over
`Logging:SlowQueryThresholdMs` (default 300 ms), a `SLOW QUERY` flag and a
`Suggested Optimization`.

Producer: `Infrastructure/Observability/FileLogging/` — a `DbCommandInterceptor`
enqueues onto a lock-free `QueryLogSink`; a `BackgroundService` flushes every
2 s so the hot path is never blocked on file I/O. Endpoint + correlation id
flow in from `CorrelationIdMiddleware` via an `AsyncLocal` (`CurrentRequestContext`).

Toggle with `Logging:EnableQueryLogging` (default **true** in Development).

## `logs/build-errors/build-error-dd-MM-yyyy.txt`

Written by `scripts/build-with-logs.sh` (repo root) — wraps `dotnet build`,
parses each `CSxxxx` / `NUxxxx` diagnostic into a structured block: project,
file, line, column, code, message, and a suggested fix for the common ones.
Use it in place of `dotnet build` when you want the failures captured.

## Adding the file logs to another service

Copy `Observability/FileLogging/` + `Diagnostics/RuntimeErrorLogWriter.cs` from
booking-service, swap the namespace + `Service` constant, register the sink +
writer in `AddInfrastructure`, add the interceptor in `AddDatabase`, and wrap
`Program.cs` startup in the try/catch. ~15 minutes.
