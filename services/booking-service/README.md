# Booking Service

Owns trip search, seat inventory, and the booking lifecycle for the Bus
Ticketing platform. First fully-implemented vertical slice of the wider
[Enterprise Transport Platform](../../MASTER_SPEC.md) roadmap. Targets
**.NET 10**.

📚 **New here?** Start with [`docs/RUNBOOK.md`](../../docs/RUNBOOK.md) for a
step-by-step from clone to a working booking, or
[`docs/README.md`](../../docs/README.md) for the full documentation index
(architecture, C4 diagrams, ERD, CRUD guides, observability guide).

## Architecture

Clean Architecture (dependency rule: Api -> Infrastructure -> Application -> Domain)
combined with vertical slices inside Application — each use case
(`SearchTrips`, `CreateBooking`, `CancelBooking`, `GetBookingById`) is a
self-contained folder with its request, validator, handler and DTO. See
[`docs/diagrams/C4_Component.md`](../../docs/diagrams/C4_Component.md) for
the diagram and [`docs/CRUD_GUIDE_BACKEND.md`](../../docs/CRUD_GUIDE_BACKEND.md)
for how to add a new feature following this pattern.

```
src/
  BookingService.Domain          Entities, value objects, domain events — zero framework dependencies
  BookingService.Application     CQRS handlers (MediatR), FluentValidation, ports (interfaces)
  BookingService.Infrastructure  EF Core + Npgsql, Redis cache-aside, transactional outbox, RabbitMQ, OTel metrics
  BookingService.Api             Minimal API endpoints, JWT auth, Serilog+Seq, native OpenAPI + Scalar, ProblemDetails
tests/
  BookingService.UnitTests         Domain + handler tests (xUnit, FluentAssertions, EF InMemory, NSubstitute)
  BookingService.IntegrationTests  Full-stack tests against real Postgres/RabbitMQ/Redis via Testcontainers
performance-tests/
  k6/          Primary, CI-friendly load + stress test scripts
  jmeter/      Same coverage, JMeter .jmx test plan
  nbomber/     Same coverage, .NET-native (NBomber) test project
```

## What's actually implemented (not stubbed)

- **Seat-safe booking**: `Trip.HoldSeats` enforces "no double allocation" as a
  domain invariant; `CreateBookingHandler` additionally relies on optimistic
  concurrency (Postgres `xmin` mapped onto `Trip.Version`) so two concurrent
  requests racing for the same seat can't both win. Proven under 50
  concurrent virtual users by `performance-tests/*/create-booking-stress-test.*`,
  not just a single-threaded unit test.
- **Transactional outbox** → RabbitMQ (see `docs/diagrams/Sequence_Diagrams.md`).
- **Redis cache-aside** on `SearchTrips` (30s TTL, evicted on write, fails open).
- **CQRS via MediatR** — commands mutate aggregates through domain invariants; queries read via direct EF Core projections.
- **Pagination**: every list endpoint defaults to page 1 / 10 results if
  omitted, and returns metadata in an `X-Pagination` response header — see
  [`docs/api/API_PAGINATION.md`](../../docs/api/API_PAGINATION.md).
- **Observability**: OpenTelemetry traces (OTLP → Jaeger) and metrics
  (Prometheus scrape at `/metrics` → Grafana dashboard), structured logs to
  console + Seq, custom business metrics (`bookings_created_total`,
  `booking_seat_conflicts_total`, ...). Full how-to-query walkthrough in
  [`docs/OBSERVABILITY_GUIDE.md`](../../docs/OBSERVABILITY_GUIDE.md).
- **API docs**: native OpenAPI document at `/openapi/v1.json`, rendered
  interactively by **Scalar** at `/scalar` — click any endpoint, "Try it".
  Real request/response examples live in
  [`docs/api/API_EXAMPLES.md`](../../docs/api/API_EXAMPLES.md) rather than
  inline in the endpoint code (see the note below on why Swagger isn't used).
  Plus a Postman collection (`../../postman/`) whose pre-request script
  mints and attaches a bearer token to every request automatically.
- **Validation & error shape**: FluentValidation runs as a MediatR pipeline
  behavior; `ExceptionHandlingMiddleware` translates domain exceptions into
  RFC 7807 `ProblemDetails`.

### Why Scalar only, not Swagger + Scalar

On .NET 10, Swashbuckle (still built against OpenAPI.NET v1) and the
framework's own `Microsoft.AspNetCore.OpenApi` (now on OpenAPI.NET v2)
disagree on the shape of `OpenApiDocument`/`OpenApiSchema`. Registering both
in the same app throws at startup — the exact "builds fine, crashes on run"
symptom this repo hit after the .NET 10 upgrade. Scalar reads the native
`/openapi/v1.json` document directly, so it doesn't need Swashbuckle at all.
See the comment above the OpenAPI registration in `Program.cs`.

## What's intentionally out of scope for this slice

Payment processing, notification delivery, RBAC beyond a bearer-token check,
multi-tenancy, and the other 7 services in `MASTER_SPEC.md` are not built
here — see `ROADMAP.md` for sequencing, and the root README's "Known gaps"
for the current honest list (Auth Service included).

## Running locally

**Full step-by-step**: [`docs/RUNBOOK.md`](../../docs/RUNBOOK.md). Short version:

```bash
# Whole stack: Postgres, RabbitMQ, Redis, Seq, Jaeger, Prometheus, Grafana, both frontends
cd infrastructure/docker
docker compose up --build

# Or just the backend, locally (needs migrations generated first — see RUNBOOK.md step 2):
cd services/booking-service
dotnet restore
dotnet ef migrations add InitialCreate --project src/BookingService.Infrastructure --startup-project src/BookingService.Api
dotnet ef database update --project src/BookingService.Infrastructure --startup-project src/BookingService.Api
dotnet run --project src/BookingService.Api
```

| What | Where |
|---|---|
| Scalar API reference | http://localhost:8080/scalar |
| Raw OpenAPI document | http://localhost:8080/openapi/v1.json |
| Postman collection | `../../postman/` |
| Seq (structured logs) | http://localhost:8081 |
| Jaeger (traces) | http://localhost:16686 |
| Prometheus (raw metrics) | http://localhost:9090 |
| Grafana (dashboards) | http://localhost:3000 (admin/admin) |
| RabbitMQ management | http://localhost:15672 (guest/guest) |

## Load & stress testing

Three equivalent options — pick whichever fits your team's toolchain, each
has its own README with exact run commands:

```bash
cd performance-tests/k6      && cat README.md   # primary, CI-friendly
cd performance-tests/jmeter  && cat README.md   # GUI/JMeter-standardized teams
cd performance-tests/nbomber && cat README.md   # .NET-native
```

## Running tests

```bash
dotnet test tests/BookingService.UnitTests
dotnet test tests/BookingService.IntegrationTests   # needs Docker (Postgres, RabbitMQ, Redis via Testcontainers)
```
