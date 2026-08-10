# Route Service — Architecture

Same Clean Architecture + CQRS conventions as Auth, Booking, and Bus
Service — see `docs/architecture/auth-service-architecture.md` §1 for the
full rationale, not repeated here. This doc covers what's specific to Route
Service.

## 1. What Route Service owns

The canonical (source-of-truth) record for every route, stop, and schedule
in the Enterprise Transport Platform. Booking Service references routes and
stops by ID; Bus Service references routes by ID. Neither stores their own
copy — Route Service is the single owner.

**Domain model:**

- **Stop** — physical location (code, name, city, address, lat/lng). Soft-
  delete supported. Global unique index on `Code`.
- **Route** — ordered set of stops with origin, destination, transport mode,
  distance, estimated duration, and lifecycle status. `RouteStop` is the
  join entity carrying `StopOrder` and optional time offsets.
- **Schedule** — time-bounded activation window for a route (departure,
  arrival, planned/active/suspended/completed). Optimistic concurrency via
  `Version`.

## 2. Route lifecycle

```
Draft ──► Active ──► Suspended ──► Active
  │         │           │
  │         │           ▼
  │         │       Deprecated
  │         ▼
  │     Deprecated
  ▼
Deprecated (soft-deleted)
```

Enforced in the domain (`Route.ChangeStatus`). `Draft` → `Active` is the
happy path. `Active` ↔ `Suspended` are reversible. Either can move to
`Deprecated`. Soft-deleted routes are marked `Deprecated` + `IsDeleted = true`
and can be restored back to `Draft`.

## 3. Schedule lifecycle

```
Planned ──► Active ──► Suspended ──► Active
                │
                ▼
            Completed
```

Only `Planned` schedules can be activated. `Active` schedules can be
suspended or completed. `Suspended` schedules can return to `Active`.
`Completed` is terminal. Soft-delete is always allowed.

## 4. Database portability

Same three-provider switch as every other backend service
(`Database:Provider` = Postgres | SqlServer | MySql). Migrations are
provider-specific; generate one for whichever provider you deploy. Route
Service uses optimistic concurrency on `Route.Version` and `Schedule.Version`
(`IsConcurrencyToken()`), which is portable across all three providers.

## 5. Stop soft-delete and query behavior

`Stop` does **not** use a global query filter (removed to avoid the EF Core
"required navigation with query filter" warning when `RouteStop.StopId` is
required). Handlers filter `!s.IsDeleted` explicitly. This keeps the model
clean and avoids the required/optional FK ambiguity.

## 6. Observability

Same platform-wide observability stack as Bus Service:

- **Serilog** structured logging to console.
- **OpenTelemetry** distributed tracing (AspNetCore, EF Core, HTTP, runtime)
  with OTLP export.
- **Prometheus** metrics via `/metrics`.
- **Custom business metrics** (`RouteMetrics`): routes created, schedules
  activated, etc.
- **Health checks** at `/health` (Postgres, Redis, RabbitMQ).
- **File-based diagnostics**:
  - `logs/runtime-error-<dd-MM-yyyy>.txt` — startup/crash logs written by
    `RuntimeErrorLogWriter`.
  - `logs/query-log-<dd-MM-yyyy>.txt` — SQL statements + duration + endpoint
    correlation (off by default; enable via `Logging:EnableQueryLogging`).

## 7. API surface

REST endpoints under `/api/v1/` with Scalar docs at `/scalar` (Development
only). JWT Bearer authentication required on all endpoints. Rate limiting
applied to write endpoints.

gRPC service `RouteGrpcServiceImpl` exposed for low-latency consumers
(mobile apps, gateway).

## 8. Event catalog

| Event | Purpose |
|-------|---------|
| `RouteCreatedDomainEvent` | New route registered |
| `RouteUpdatedDomainEvent` | Route details changed |
| `RouteStatusChangedDomainEvent` | Route lifecycle transition |
| `StopCreatedDomainEvent` | New stop added |
| `StopUpdatedDomainEvent` | Stop details changed |
| `ScheduleCreatedDomainEvent` | New schedule created |
| `ScheduleUpdatedDomainEvent` | Schedule times changed |
| `ScheduleActivatedDomainEvent` | Schedule moved to Active |
| `ScheduleSuspendedDomainEvent` | Schedule suspended |

All events flow through the transactional outbox (`outbox_messages` table)
and are published to the `route.events` RabbitMQ exchange by
`OutboxProcessor`.

## 9. Known gaps

- **No Booking Service sync consumer yet.** Domain events are ready to be
  consumed; the consumer side isn't built.
- **No plate-number or operator-transfer endpoint** (N/A for Route Service
  but noted for parity).
- **No load/stress test scripts** checked in yet (k6/JMeter/NBomber folders
  exist but are empty).
