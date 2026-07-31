# Booking Service

Owns trip search, seat inventory, and the booking lifecycle for the Bus
Ticketing platform. First fully-implemented vertical slice of the wider
[Enterprise Transport Platform](../../MASTER_SPEC.md) roadmap. Targets
**.NET 10**.

## Architecture

Clean Architecture (dependency rule: Api -> Infrastructure -> Application -> Domain)
combined with vertical slices inside Application — each use case
(`SearchTrips`, `CreateBooking`, `CancelBooking`, `GetBookingById`) is a
self-contained folder with its request, validator, handler and DTO, instead
of being scattered across generic `Services/` and `Repositories/` folders.

```
src/
  BookingService.Domain          Entities, value objects, domain events — zero framework dependencies
  BookingService.Application     CQRS handlers (MediatR), FluentValidation, ports (interfaces)
  BookingService.Infrastructure  EF Core + Npgsql, Redis cache-aside, transactional outbox, RabbitMQ, OTel metrics
  BookingService.Api             Minimal API endpoints, JWT auth, Serilog+Seq, Swagger + Scalar/OpenAPI, ProblemDetails
tests/
  BookingService.UnitTests         Domain + handler tests (xUnit, FluentAssertions, EF InMemory, NSubstitute)
  BookingService.IntegrationTests  Full-stack tests against real Postgres/RabbitMQ/Redis via Testcontainers
  load/                            k6 load test (search) + stress test (concurrent seat-booking correctness)
```

## What's actually implemented (not stubbed)

- **Seat-safe booking**: `Trip.HoldSeats` enforces "no double allocation" as a
  domain invariant; `CreateBookingHandler` additionally relies on optimistic
  concurrency (Postgres `xmin` mapped onto `Trip.Version`) so two concurrent
  requests racing for the same seat can't both win, even under real load —
  the loser gets a `409 Conflict` (`SeatUnavailableException`), not a corrupt
  booking. `tests/load/create-booking-stress-test.js` proves this under 50
  concurrent virtual users, not just a single-threaded unit test.
- **Transactional outbox**: domain events are written to the `outbox_messages`
  table in the *same* database transaction as the aggregate change, then
  relayed to RabbitMQ by a background `OutboxProcessor`.
- **Redis cache-aside**: `SearchTrips` results are cached for 30s per query
  (the highest-traffic, most-repeated read on the platform); `CreateBooking`
  / `CancelBooking` evict the cache on write so seat counts don't go stale.
  The cache fails open — a Redis outage degrades to "always hits Postgres",
  never a 500.
- **CQRS via MediatR**: commands mutate aggregates through domain invariants;
  queries read via direct EF Core projections.
- **Observability**: OpenTelemetry traces (OTLP -> Jaeger) and metrics
  (Prometheus scrape at `/metrics` -> Grafana dashboard), structured logs to
  both console and Seq, plus custom business metrics
  (`bookings_created_total`, `booking_seat_conflicts_total`, ...) — see
  `infrastructure/monitoring/`.
- **API docs you can actually click and run**: Swagger UI at `/swagger` and
  a modern OpenAPI reference (Scalar) at `/scalar`, both with real filled-in
  request/response examples — not empty schemas. Plus a Postman collection
  (`../../postman/`) whose pre-request script mints and attaches a bearer
  token to every request automatically.
- **Validation & error shape**: FluentValidation runs as a MediatR pipeline
  behavior; `ExceptionHandlingMiddleware` translates domain exceptions into
  RFC 7807 `ProblemDetails`.

## What's intentionally out of scope for this slice

Payment processing, notification delivery, RBAC beyond a bearer-token check,
multi-tenancy, and the other 7 services in `MASTER_SPEC.md` are not built
here — see `ROADMAP.md` for sequencing.

## Running locally

Requires the .NET 10 SDK, Node 22, and Docker. This was developed with real
compilation and package restore this time around (the initial vertical slice
was hand-written in a sandbox with no SDK access — see git history); still,
review before relying on it, especially the OpenTelemetry Redis
instrumentation call flagged with a comment in `Program.cs`, which is
version-sensitive.

```bash
# Whole stack: Postgres, RabbitMQ, Redis, Seq, Jaeger, Prometheus, Grafana, both frontends
cd infrastructure/docker
docker compose up --build

# Or just the backend, locally:
cd services/booking-service
dotnet restore
dotnet ef migrations add InitialCreate \
  --project src/BookingService.Infrastructure --startup-project src/BookingService.Api
dotnet ef database update \
  --project src/BookingService.Infrastructure --startup-project src/BookingService.Api
dotnet run --project src/BookingService.Api
```

Then seed some data and try it:

```bash
psql "postgresql://booking_svc:changeme@localhost:5432/booking_service" -f ../../scripts/seed-demo-data.sql
```

| What | Where |
|---|---|
| Swagger UI | http://localhost:8080/swagger |
| Scalar API reference | http://localhost:8080/scalar |
| Raw OpenAPI document | http://localhost:8080/openapi/v1.json |
| Postman collection | `../../postman/` (see its README for the auto-bearer-token trick) |
| Seq (structured logs) | http://localhost:8081 |
| Jaeger (traces) | http://localhost:16686 |
| Prometheus (raw metrics) | http://localhost:9090 |
| Grafana (dashboards) | http://localhost:3000 (admin/admin) |
| RabbitMQ management | http://localhost:15672 (guest/guest) |

## Load & stress testing

```bash
cd tests/load
k6 run -e BASE_URL=http://localhost:8080 search-trips-load-test.js
k6 run -e BASE_URL=http://localhost:8080 -e TRIP_ID=<seeded-trip-id> -e ACCESS_TOKEN=<dev-jwt> create-booking-stress-test.js
```

See `tests/load/README.md` for what each threshold means and how to get a
dev JWT (the Postman collection's pre-request script generates one the same
way, or use it directly).

## Running tests

```bash
dotnet test tests/BookingService.UnitTests
dotnet test tests/BookingService.IntegrationTests   # needs Docker (Postgres, RabbitMQ, Redis via Testcontainers)
```
