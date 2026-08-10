# Route Service — Architecture

## 1. Overview

Route Service manages the transport network topology: **Routes**, **Stops**, and **Schedules**.
It owns the canonical (source-of-truth) records for these aggregates and publishes
domain events via RabbitMQ for downstream services (Booking, Notification, etc.).

## 2. Layering

| Layer | Responsibility |
|-------|---------------|
| **Domain** | Entities, enums, domain events, exceptions. Zero framework dependencies. |
| **Application** | CQRS commands/queries, validators, behaviors, DTOs, Result pattern, localization interfaces. |
| **Infrastructure** | EF Core DbContext, RabbitMQ publisher, Redis cache, audit logging, Polly resilience, gRPC server. |
| **Api** | Minimal API endpoints, gRPC services, middleware (correlation, exception handling, localization), Serilog, OpenTelemetry. |

## 3. Domain Model

### Route (Aggregate Root)
- `Code` (unique), `Name`, `OriginStopId`, `DestinationStopId`, `TransportMode`, `DistanceKm`, `EstimatedDuration`, `Status`
- State machine: `Draft → Active ↔ Suspended → Deprecated`
- Supports optimistic concurrency via `Version` (mapped as concurrency token).
- Soft-deletable (`IsDeleted`).
- Contains ordered `RouteStop` junctions.

### Stop (Aggregate Root)
- `Code` (unique), `Name`, `City`, `Address`, `Latitude`, `Longitude`
- Soft-deletable.
- Prevents deletion when referenced by active routes.

### Schedule (Aggregate Root)
- `RouteId`, `DepartureTime`, `ArrivalTime`, `Status`, `EffectiveFrom`, `EffectiveTo`
- State machine: `Planned → Active ↔ Suspended → Completed`
- Optimistic concurrency via `Version`.

## 4. Database Portability

`Database:Provider` (`Postgres` | `SqlServer` | `MySql`) selects the EF Core provider at startup.
Migrations are provider-specific; switching providers requires regenerating migrations.

## 5. Localization

- English (`en`) is the default fallback.
- Bangla (`bn`) is supported via `.resx` resource files.
- Adding a third language requires only a new `Messages.<culture>.resx` file.

## 6. gRPC

Internal `RouteGrpcService` exposes:
- `GetRoute` — synchronous route lookup.
- `SearchRoutes` — paginated search.

gRPC is intended for service-to-service calls (e.g., Booking Service needs a route reference).

## 7. Resilience

- **Retry**: Polly retry policy (3 attempts, exponential backoff) on HTTP communication.
- **Circuit Breaker**: Polly advanced circuit breaker (50% failure threshold, 30s sampling).
- **Timeout**: 10s timeout policy.
- **Rate Limiting**: Fixed-window limiter on write endpoints (20 requests / 10s).

## 8. Audit Logging

Every create/update/delete is logged to the `audit_logs` table and to Serilog
with user ID, correlation ID, and IP address.

## 9. Soft Delete

All major aggregates implement `IsDeleted` / `DeletedAtUtc`. Global query filters
hide soft-deleted rows from reads. REST endpoints support restore.

## 10. Event-Driven

Domain events are persisted to an `outbox_messages` table within the same transaction
as the aggregate write. `OutboxProcessor` (background service) publishes them to
the `route.events` RabbitMQ topic exchange with retry logic.

## 11. Release Information Endpoint

`GET /api/v1/release/info` returns service metadata, build number, commit, and
feature flags for SQA/testers.
