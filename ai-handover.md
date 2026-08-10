# AI Handover — Enterprise Transport Platform

**Read this first.** Written so any AI assistant (or you, working solo) can
pick this repo up cold and know exactly what's real, what's scaffold, and
what to do next — without re-discovering it turn by turn the way this
session did.

Last updated: end of the session that built Bus Service. `git log
--oneline` is the source of truth for exact history; this doc is the
narrative summary of it.

---

## 1. Status at a glance

| Service | Status | Notes |
|---|---|---|
| **Auth Service** | ✅ Built, fixed, verified against a real `dotnet build`/`dotnet run` | Register/login/refresh (rotation + theft detection)/logout, audit trail, multi-provider DB |
| **Booking Service** | ✅ Built, fixed | Trip search, seat-hold booking, had a regression once (see §5) — currently restored |
| **Bus Service** | ✅ Built this session, **not yet run against a real compiler** | Fleet/depot management — see §3 |
| **Route Service** | ✅ Built, fixed, verified against a real `dotnet build` | Routes, stops, schedules — see §3 |
| **Payment Service** | ❌ Not started | Empty folder (`services/payment-service`) |
| **Notification Service** | ❌ Not started | Empty folder (`services/notification-service`) |
| **Angular customer web app** | ✅ Built for demo | Mock/fake API responses — not wired to real backend yet |
| **React admin console** | ✅ Built for demo | Mock/fake API responses — not wired to real backend yet |
| **Mobile (Flutter/MAUI/Native)** | ❌ Not started | Empty scaffold folders under `apps/` |
| **API Gateway** | ❌ Not started | Empty folder (`infrastructure/gateway`) |
| **docs/ (SRS, ADRs, diagrams, etc.)** | 📋 Planning scaffold | Extensive pre-existing reference docs (see §7) — largely written before any code, treat as intent/reference, not as a description of current reality |

**No .NET SDK or network access exists in the environment any of this was
built in — nothing has ever been compiled by the AI that wrote it.**
Every fix and every new service was verified by careful static
review (brace-balance, cross-referencing real `dotnet build` output
where the user provided it, checking against the exact bug classes
already found) — not by an actual compiler, except where the user
pasted real `dotnet build`/`dotnet run` output back for verification
(Auth Service — see §4). **Run a real `dotnet build` on Bus Service
before trusting it.**

---

## 2. What actually works right now (verified against real output)

**Auth Service** is the only service that has been round-tripped against
a real `dotnet build` *and* `dotnet run` on the user's machine, with the
resulting errors pasted back and fixed. As of the last verification:
build succeeded, and the last reported runtime issue (an EF Core
"DomainEvents requires a primary key" crash) was fixed. See
`docs/architecture/auth-service-architecture.md` §13 for the full
list of every bug found and fixed, with root causes — **read this before
touching any other service**, because most of those bugs were later found
to also exist in Booking Service, and were proactively avoided when
writing Bus Service. It's the single most useful file in this repo for
avoiding repeat work.

**Booking Service**: built, then regressed (a merge in the user's own
workflow reverted it to a pre-fix state — see §5), then restored + re-fixed
in this session. Not re-verified against a real build since the restore.

**Route Service**: fully written this session (all four Clean Architecture
layers + tests + docs + generated EF Core migration), applying every
lesson from Auth/Booking/Bus's fix history. Build verified; 28 unit tests
passing. Never run end-to-end (requires Postgres/Redis/RabbitMQ).

**Bus Service**: fully written this session (all four Clean Architecture
layers + tests + docs + a hand-authored EF Core migration), applying every
lesson from Auth/Booking's fix history from the start. **Never run.**

---

## 3. Bus Service — what's in it

New this session. Canonical source of truth for fleet (`Bus`) and
`Depot` data — Booking Service already has a read-only replica of
`OperatorId`/`PlateNumber`/`BusType`/`TotalSeats` explicitly commented
"owned by the Bus Service in production" (see
`services/booking-service/src/BookingService.Domain/Entities/Bus.cs`).

- **Domain**: `Bus` (aggregate root, enforced lifecycle: Active ↔
  UnderMaintenance, either → Retired, Retired is terminal), `Depot`.
- **Application**: RegisterBus, GetBus, GetBuses (paginated/filterable),
  UpdateBusDetails, ChangeBusStatus, CreateDepot, GetDepots — CQRS via
  MediatR, same convention as Auth/Booking.
- **Infrastructure**: EF Core, **DB-provider-switchable** (Postgres /
  SqlServer / MySQL via `Database:Provider` config — matches Auth
  Service's pattern, per explicit instruction to keep every backend
  service this way). Transactional outbox → RabbitMQ (`bus.events`).
  Redis cache-aside. A **hand-authored** EF Core migration (no SDK to
  generate a real one — see `docs/development/database-migrations.md`
  for the caveat and how to regenerate a verified one).
- **Api**: native OpenAPI + Scalar (not Swashbuckle — see §4), JWT bearer
  auth (validates tokens from Auth Service — same signing key config),
  health checks, OpenTelemetry.
- **New platform-wide feature, introduced here**: file-based diagnostic
  logging — `logs/build-error-<dd-MM-yyyy>.txt`,
  `logs/runtime-error-<dd-MM-yyyy>.txt`, `logs/query-log-<dd-MM-yyyy>.txt`
  (SQL statement + duration + triggering endpoint, for finding queries to
  optimize). See `scripts/README.md` for the full design. **Only wired
  into Bus Service so far** — Auth and Booking Service do not have this
  yet (see §6, "Next steps").
- **Tests**: unit tests (Domain lifecycle rules, RegisterBus/ChangeBusStatus
  handlers), integration test skeleton (Testcontainers — mints its own JWT
  locally since Bus Service has no login endpoint of its own).
- **Docs**: `docs/architecture/bus-service-architecture.md`,
  `services/bus-service/README.md`.

**Known gaps** (see the architecture doc's §6 for the full list): no
plate-number/operator-transfer endpoint, no Booking Service sync consumer
for the events this service publishes yet, no rate limiting.

---

## 4. Bug classes already found — check for these first in any new service

Discovered the hard way (real `dotnet build`/`dotnet run` output from the
user), documented in detail in
`docs/architecture/auth-service-architecture.md` §13. Before writing or
debugging any service, check for these:

1. **A feature-folder namespace with the same name as a Domain entity**
   (e.g. `Features.Auth.RefreshToken` vs the `RefreshToken` entity) causes
   C# to resolve the bare type name to the *namespace*, not the type —
   `CS0234`. Fully-qualify (`Domain.Entities.X`) whenever a feature folder
   shares a name with an entity.
2. **`await` is not allowed in a `catch (...) when (...)` filter** —
   `CS7094`. Do the async check inside the catch body instead, with a
   conditional `throw;`.
3. **`Microsoft.AspNetCore.RateLimiting`** (`AddRateLimiter`/
   `UseRateLimiter`/`RequireRateLimiting`) is **not** in the ASP.NET Core
   Web SDK's implicit-usings list — needs an explicit `using`.
4. **`IMeterFactory`** lives in `Microsoft.Extensions.Diagnostics.Metrics`,
   not `System.Diagnostics.Metrics` — a plain `Microsoft.NET.Sdk` class
   library (Infrastructure projects) gets *no* ASP.NET Core implicit
   usings at all, so this is easy to miss. Reference
   `Microsoft.Extensions.Diagnostics.Abstractions` explicitly too, don't
   rely on it arriving transitively.
5. **`AspNetCore.HealthChecks.Rabbitmq` 9.0.0 dropped its
   connection-string API** — now requires a DI-resolved `IConnection`.
   Use **`AspNetCore.HealthChecks.Rabbitmq.v6`** instead, which kept the
   `rabbitConnectionString:` string API and matches the
   `RabbitMQ.Client` 6.8.1 already pinned everywhere in this repo.
6. **`OpenTelemetry.Api` 1.10.0–1.11.1 has a real DoS advisory**
   (GHSA-8785-wc3w-h8q6) — pin every `OpenTelemetry.*` package to **1.17.0**
   (`1.17.0-beta.1` for the two contrib packages without a stable release:
   `Instrumentation.EntityFrameworkCore`, `Exporter.Prometheus.AspNetCore`).
7. **Swashbuckle + native `Microsoft.AspNetCore.OpenApi` together = Scalar
   silently shows zero endpoints** (they publish the OpenAPI document at
   different routes; Scalar's default route matches the native generator,
   not Swashbuckle). **Use native OpenAPI only** (`AddOpenApi("v1")` +
   `MapOpenApi()`), never Swashbuckle, in this platform.
8. **`AggregateRoot.DomainEvents`** (a public `IReadOnlyCollection<T>`
   property) gets auto-discovered by EF Core as a navigation property
   unless explicitly `Ignore()`d in the entity's `IEntityTypeConfiguration`
   — causes a runtime crash ("entity type 'DomainEvent' requires a primary
   key") the first time the model is actually built (e.g. on
   `Database.MigrateAsync()`), **not** at compile time. Same for
   `AggregateRoot.Version` if you're not actually mapping it to a real
   concurrency column. **`Ignore()` both, in every new
   `IEntityTypeConfiguration<T: AggregateRoot>`, from the start.**
9. **`System.Security.Cryptography.Xml`** arrives transitively (via
   `Microsoft.Data.SqlClient`, pulled in by
   `Microsoft.EntityFrameworkCore.SqlServer`) at a version with known DoS
   advisories — pin it directly to **10.0.6+** to override.
10. **Pomelo.EntityFrameworkCore.MySql has no official EF Core 10
    release** as of this writing — pin to `9.0.0` with a documented,
    scoped `NoWarn="NU1608"` on that one `PackageReference`, don't try to
    "fix" the version mismatch, it isn't fixable yet.

If you're an AI continuing this work: **read
`docs/architecture/auth-service-architecture.md` §13 in full** before
writing a new service — it has the complete root-cause writeup for each
of the above, not just this summary.

---

## 5. One regression already happened once — watch for it again

Mid-session, a `git merge`/rebase done in the user's own workflow (outside
this AI's control) silently reverted Booking Service back to a pre-fix
state — lost the native-OpenAPI fix, Redis caching, and more, without any
error or warning. It was only caught because the user pasted a `dotnet
build` log that showed old package versions. **If a service that was
previously confirmed working starts showing already-fixed bugs again,
suspect a lost merge/rebase before assuming new code broke it** — check
`git log --oneline -- <path>` for that file; if the fix commit isn't in
the current branch's ancestry, that's the regression.

---

## 6. Next steps, in priority order

1. **Compile-verify Bus Service.** Nothing in this repo has been
   confirmed to actually build except Auth Service (and Booking Service,
   indirectly, before its regression). Run:
   ```bash
   scripts/dotnet-build.sh services/bus-service/BusService.sln
   scripts/dotnet-run.sh services/bus-service/src/BusService.Api
   ```
   Paste any error back (to an AI session, or debug directly) — expect
   the ordinary handful of typos/version-pin issues a first real build
   surfaces, same as happened with Auth Service.
2. **Generate a real EF Core migration for Bus Service**, superseding the
   hand-authored one (`dotnet ef migrations add InitialCreate` — see
   `docs/development/database-migrations.md`). The hand-authored one was
   cross-checked field-by-field but is not a substitute for real
   tool output.
3. **Payment Service** — next service to build, same pattern as Route
   Service. Likely scope: payment intents, providers, refunds, webhooks —
   check `docs/database/Tables.md` and `docs/api/API_Contracts.md` for
   planning notes.
4. **Notification Service** — same pattern. Notification Service is a
   dependency for finishing two "known gaps" already flagged in Auth Service
   (email verification, password reset).
5. **Backport the file-based diagnostic logging feature** (§3, "new
   platform-wide feature") to Auth Service and Booking Service — only
   built into Bus Service so far.
6. **API Gateway** (`infrastructure/gateway`, empty) — every service
   currently trusts an `X-Forwarded-For` header it assumes a gateway sets;
   that assumption isn't true yet since no gateway exists.
7. **Wire the frontend apps to the real backends** — both `apps/`
   frontends currently use mock/fake API responses per the user's own
   description; connecting them to Auth/Booking/Bus Service is unstarted.

---

## 7. What NOT to be misled by

- **`docs/` has ~70 files** (SRS, ADRs, C4 diagrams, security/RBAC docs,
  performance/testing docs, per-AI prompt files under `docs/prompts/`,
  etc.) — this is **planning documentation, largely written before any
  code**, not a description of what's built. Treat it as intent/reference
  for scope and conventions, not as ground truth for current state — this
  handover doc and `git log` are ground truth for that.
- **`apps/` has scaffold folders** for Flutter, MAUI, Native Android,
  Native iOS, and a cross-platform "Shared UI Library" — all empty.
  Only `apps/angular-client` and `apps/react-admin` have real code.
- **`shared/`** is empty (0 files) — no shared/common package exists;
  every service duplicates its own copy of Domain/Common,
  Infrastructure/Outbox, etc. This is a deliberate convention (each
  service is independently deployable, not coupled to a shared library),
  not an oversight — see any service's architecture doc for why, if
  questioned.

---

## 8. How this repo's git history is organized

Every completed piece of work is its own commit with a detailed,
professional message explaining *why*, not just *what* — `git log
--oneline` gives the summary; `git show <hash>` gives the full reasoning
for any specific change. This convention was explicitly requested and
should continue: **one commit per completed vertical slice or fix, with a
message a future engineer (human or AI) could use to understand the
change without re-reading the diff.**
