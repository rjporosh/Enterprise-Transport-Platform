# Scripts & diagnostic logging

Three log types, across two mechanisms, so a build failure, a crash, or a
slow query all leave behind an easy-to-find, exact record instead of
whatever scrolled past in a terminal.

| Log | Written by | File | Rolls over |
|---|---|---|---|
| Build errors | `scripts/dotnet-build.sh` (external — nothing in-app can run yet during a build) | `<repo-root>/logs/build-error-<dd-MM-yyyy>.txt` | Daily, appended |
| Runtime/startup errors | The service itself (`Program.cs`'s top-level try/catch → `RuntimeErrorLogWriter`) **and**, as a redundant external safety net, `scripts/dotnet-run.sh` | `<service-root>/logs/runtime-error-<dd-MM-yyyy>.txt` (in-app) and `<repo-root>/logs/runtime-error-<dd-MM-yyyy>.txt` (script) | Daily, appended |
| Query performance | The service itself (`QueryLoggingInterceptor` → `QueryLogWriterBackgroundService`) | `<service-root>/logs/query-log-<dd-MM-yyyy>.txt` | Daily, appended |

All three are **one file per day, appended** — not one file per incident —
so a crash-looping service or a chatty endpoint doesn't flood `logs/` with
near-duplicate files; everything for a given day stays in one
chronological record.

## Why two mechanisms for runtime errors

The in-app handler (`RuntimeErrorLogWriter`, wired into every service's
`Program.cs`) is the rich one — it includes the full exception chain
*and* a best-effort plain-English diagnosis for the failure modes most
likely to mean "a dependency is down" (Postgres/SqlServer/MySQL
unreachable, Redis unreachable, RabbitMQ unreachable, port already in
use, pending/missing migrations, an EF Core model error). It's written to
that specific **service's own** `logs/` folder, because that's what a real
standalone deployment of that service would use — it has no concept of a
monorepo root.

The `dotnet-run.sh` script is the safety net: it catches *any* non-zero
exit code, including the failure modes .NET's own try/catch cannot catch
at all (a hard crash below the managed runtime). It writes to the
**repo-root** `logs/` folder, since it's a repo-wide tool, not something
that ships with any one service.

If a service crashes, check its own `logs/runtime-error-<today>.txt`
first — that's the one with the diagnosis. The repo-root copy exists in
case the in-app one somehow didn't get written.

## Query log — finding slow queries

`logs/query-log-<dd-MM-yyyy>.txt` (service-local, one line per SQL
statement actually executed):

```
[2026-08-06 09:14:22.103 UTC] (3.2ms) GET /api/v1/buses/f47ac10b-... :: SELECT b."Id", b."PlateNumber", ... FROM buses AS b WHERE b."Id" = @__busId_0
[2026-08-06 09:14:25.884 UTC] (142.7ms) GET /api/v1/buses :: SELECT COUNT(*)::int FROM buses AS b WHERE b."DepotId" = @__depotId_0
```

Each line: UTC start time, duration, the HTTP endpoint that triggered the
query (or `background` for anything running outside a request, like the
outbox processor), then the exact SQL. Grep for anything over, say,
`50.0ms` and you have your optimization candidates, with the endpoint
that needs the index or the N+1 fix right there.

**Off by default outside Development** (`Logging:EnableQueryLogging` in
`appsettings.json` — `true` in `appsettings.Development.json`) — it adds a
per-query enqueue (cheap, lock-free, non-blocking — see
`QueryLogSink`/`QueryLoggingInterceptor`) that isn't worth paying in
production unless you're actively debugging a performance issue there
too, in which case flip the flag.

## Build/run wrapper scripts

Thin wrappers around `dotnet build` / `dotnet run` that behave identically
on success, and on failure append to the log described above.

```bash
# Build
scripts/dotnet-build.sh services/bus-service/BusService.sln

# Run
scripts/dotnet-run.sh services/bus-service/src/BusService.Api
scripts/dotnet-run.sh services/bus-service/src/BusService.Api --urls=http://localhost:5201
```

Any extra arguments pass straight through to the underlying `dotnet`
command. `logs/` is gitignored — read locally right after a failure, not
committed to history.
