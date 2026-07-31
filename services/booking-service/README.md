# Booking Service

Owns trip search, seat inventory, and the booking lifecycle for the Bus
Ticketing platform. First fully-implemented vertical slice of the wider
[Enterprise Transport Platform](../../MASTER_SPEC.md) roadmap.

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
  BookingService.Infrastructure  EF Core + Npgsql, transactional outbox, RabbitMQ publisher
  BookingService.Api             Minimal API endpoints, JWT auth, Serilog, ProblemDetails
tests/
  BookingService.UnitTests         Domain + handler tests (xUnit, FluentAssertions, EF InMemory)
  BookingService.IntegrationTests  Full-stack tests against real Postgres/RabbitMQ via Testcontainers
```

## What's actually implemented (not stubbed)

- **Seat-safe booking**: `Trip.HoldSeats` enforces "no double allocation" as a
  domain invariant; `CreateBookingHandler` additionally relies on optimistic
  concurrency (Postgres `xmin` mapped onto `Trip.Version`) so two concurrent
  requests racing for the same seat can't both win, even under real load —
  the loser gets a `409 Conflict` (`SeatUnavailableException`), not a corrupt
  booking.
- **Transactional outbox**: domain events are written to the `outbox_messages`
  table in the *same* database transaction as the aggregate change, then
  relayed to RabbitMQ by a background `OutboxProcessor`. This is what makes
  "booking created" notifications reliable even if the process crashes
  between commit and publish.
- **CQRS via MediatR**: commands (`CreateBooking`, `CancelBooking`) mutate
  aggregates and go through domain invariants; queries (`SearchTrips`,
  `GetBookingById`) read via direct EF Core projections, no aggregate
  overhead.
- **Validation & error shape**: FluentValidation runs as a MediatR pipeline
  behavior before any handler executes; `ExceptionHandlingMiddleware`
  translates domain exceptions into consistent RFC 7807 `ProblemDetails`.

## What's intentionally out of scope for this slice

Payment processing, notification delivery, RBAC beyond a bearer-token check,
multi-tenancy, and the observability stack (OTel/Grafana) are called out in
`MASTER_SPEC.md` but not built here — see `ROADMAP.md` for sequencing. Building
them as empty scaffolding wouldn't have demonstrated anything; this slice is
depth-first on one real path instead.

## Running locally

Requires the .NET 9 SDK, Docker (for Postgres/RabbitMQ), and was **not**
compiled in the environment that generated it — no dotnet SDK or package
registry access was available there. Review before relying on it; the
Testcontainers-based integration tests in particular are written to run
locally/in CI, not verified in-sandbox.

```bash
# from services/booking-service
docker compose -f ../../infrastructure/docker/docker-compose.yml up -d postgres rabbitmq
dotnet restore
dotnet ef database update --project src/BookingService.Infrastructure --startup-project src/BookingService.Api
dotnet run --project src/BookingService.Api
# Swagger UI at http://localhost:8080/swagger
```

Run tests:

```bash
dotnet test tests/BookingService.UnitTests
dotnet test tests/BookingService.IntegrationTests   # needs Docker
```

## Adding an EF Core migration

No migrations are checked in yet (this environment couldn't run `dotnet ef`).
Generate the initial one locally with:

```bash
dotnet ef migrations add InitialCreate \
  --project src/BookingService.Infrastructure \
  --startup-project src/BookingService.Api \
  --output-dir Persistence/Migrations
```
