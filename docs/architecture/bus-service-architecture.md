# Bus Service — Architecture

Same Clean Architecture + CQRS conventions as Auth and Booking Service —
see `docs/architecture/auth-service-architecture.md` §1 for the full
rationale, not repeated here. This doc covers what's specific to Bus
Service.

## 1. What Bus Service owns

The canonical (source-of-truth) record for every vehicle in the fleet and
the depots they're based out of. Booking Service already keeps a
read-only, denormalized replica of the fields it needs — `OperatorId`,
`PlateNumber`, `BusType`, `TotalSeats` — explicitly commented in that
service's `Entities/Bus.cs` as "owned by the Bus Service in production."
Bus Service is that production owner: `BusRegisteredDomainEvent` /
`BusDetailsUpdatedDomainEvent` / `BusStatusChangedDomainEvent` (published
via the same transactional-outbox → RabbitMQ pattern as the other two
services, exchange `bus.events`) are what a future sync consumer in
Booking Service would subscribe to, to keep that replica current.

`OperatorId` is a reference to a User in Auth Service (one with the
`Operator` role) — a plain `Guid`, no foreign key, same cross-service
decoupling convention used everywhere else in this platform.

## 2. Bus lifecycle

```
Active <──────────────────┐
  │  \                     │
  │   \                    │
  ▼    ▼                   │
UnderMaintenance ──────────┘
  │
  ▼
Retired  (terminal — no transitions out)
```

Enforced in the domain (`Bus.ChangeStatus`), not left to callers: `Active`
and `UnderMaintenance` transition freely between each other; either can
move to `Retired`; `Retired` is one-way. A retired bus re-entering the
fleet is modeled as a **new registration** (new plate-number check, new
`Id`), not a status transition back out of `Retired` — this keeps the
audit history of the retired vehicle intact rather than reusing/mutating
it, and matches how a real fleet operation would actually treat a
decommissioned vehicle coming back into service (typically re-inspected
and re-registered, not just "turned back on").

## 3. Database portability

Same three-provider switch as Auth Service (`Database:Provider` = Postgres
| SqlServer | MySql), applied here per instruction to keep every backend
service consistent rather than letting Booking Service's Postgres-only
approach be the odd one out going forward. See
`docs/architecture/auth-service-architecture.md` §8 for the full
rationale — migrations are still provider-specific, Oracle is still
documented-not-wired, and `Bus.Version` is `Ignore()`d for the same
"no single concurrency-column strategy is portable across all three"
reason `User.Version` is in Auth Service.

## 4. File-based diagnostic logging

New with this service (see `scripts/README.md` for the full picture
across the whole platform) — three log types, so a build failure, a
crash, or a slow query all leave an exact, easy-to-find record:

- **`logs/build-error-<dd-MM-yyyy>.txt`** — written by
  `scripts/dotnet-build.sh` (external; nothing in-app can run yet during a
  build failure).
- **`logs/runtime-error-<dd-MM-yyyy>.txt`** — written by this service
  itself. `Program.cs` wraps its *entire* startup and `app.Run()` in one
  try/catch feeding `Api/Diagnostics/RuntimeErrorLogWriter`, which
  includes a best-effort plain-English diagnosis for the failure modes
  that mean "a dependency is down" — Postgres/SqlServer/MySQL, Redis,
  RabbitMQ unreachable; port already in use; pending/missing migrations;
  an EF Core model error (the exact class of bug §6 below almost
  reintroduced). `scripts/dotnet-run.sh` writes a second, plainer copy to
  the repo-root `logs/` as an external safety net for crash modes .NET's
  own try/catch can't catch at all.
- **`logs/query-log-<dd-MM-yyyy>.txt`** — every SQL statement actually
  executed, with its start time, duration, and the HTTP endpoint that
  triggered it. Captured by `QueryLoggingInterceptor` (an EF Core
  `DbCommandInterceptor`), queued through a lock-free `ConcurrentQueue` so
  it adds no measurable latency to the query itself, and flushed to disk
  every 2 seconds by `QueryLogWriterBackgroundService`. The endpoint
  correlation flows through `CurrentRequestContext` — an `AsyncLocal<string>`
  set by `RequestContextMiddleware` at the start of every request —
  specifically so Infrastructure never needs an `IHttpContextAccessor` /
  ASP.NET Core `FrameworkReference` just for this. Off by default outside
  Development (`Logging:EnableQueryLogging`), since it's a diagnostic tool
  for finding queries worth optimizing, not something to pay the (small,
  but nonzero) overhead for at full production QPS by default.

## 5. Why native OpenAPI, not Swashbuckle

Applied from the start here — see Auth/Booking Service's own architecture
docs for the full story of the bug this avoids (Scalar loading with zero
endpoints shown, because Swashbuckle and the native OpenAPI.NET v2-based
generator publish their documents at different routes, and only one of
them is what Scalar actually asks for by default).

## 6. Known gaps

- **No plate-number or operator-transfer endpoint.** `UpdateBusDetails`
  deliberately can't change `PlateNumber` or `OperatorId` — see
  `Bus.UpdateDetails`'s doc comment. Both are rarer, higher-stakes
  operations that would want their own audit trail; not built.
- **No Booking Service sync consumer yet.** The domain events this service
  publishes are ready to be consumed; the consumer side (in Booking
  Service, or a dedicated sync worker) isn't built.
- **EF Core migrations aren't pre-committed** — provider-specific, same as
  every other service here. Generate one for whichever provider you
  deploy (see README, "Running locally").
- **No rate limiting** on the write endpoints, unlike Auth Service's
  `/register`/`/login`. Bus Service sits behind authentication for every
  endpoint already (unlike Auth's necessarily-anonymous auth endpoints),
  and fleet-management writes are a low-volume, internal-operator action —
  judged not worth the added complexity for this service. Revisit if this
  service ever gains a public-facing write path.
