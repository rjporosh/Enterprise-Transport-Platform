# Route Service - Release Notes

## v1.0.0 — Initial Release

### Features

- CRUD for **Stops** (create, read, update, soft-delete, restore)
- CRUD for **Routes** with lifecycle management (Draft, Active, Suspended, Deprecated)
- **RouteStop** join entity with ordered stops and optional time offsets
- CRUD for **Schedules** with lifecycle (Planned, Active, Suspended, Completed)
- Soft-delete + restore for Routes, Stops, and Schedules
- Optimistic concurrency on Route and Schedule versions
- JWT Bearer authentication (validates against Auth Service signing key)
- Role-based authorization (Admin, Operator)
- Rate limiting on write endpoints
- REST API with Scalar interactive docs (`/scalar`)
- gRPC service (`RouteGrpcServiceImpl`)
- Health checks (PostgreSQL, Redis, RabbitMQ)
- Structured logging via Serilog
- OpenTelemetry distributed tracing
- Prometheus metrics (`/metrics`)
- Custom business metrics (routes created, schedules activated)
- Redis cache-aside for route lookups
- Transactional outbox → RabbitMQ (`route.events` exchange)
- Domain events for all state changes
- FluentValidation for all commands
- Bangla / English localization support
- File-based diagnostic logging (runtime errors, query logs)
- Correlation ID middleware
- Exception handling middleware with plain-English diagnostics
- Database provider switch (PostgreSQL / SQL Server / MySQL)

### Database

- EF Core `InitialCreate` migration scaffolded
- Multi-provider support via `Database:Provider` config
- Schema: `route`
- Tables: `routes`, `stops`, `route_stops`, `schedules`, `audit_logs`, `outbox_messages`

### Testing

- 28 unit tests covering:
  - Route CRUD + status transitions
  - Stop CRUD
  - Schedule CRUD + status transitions (activate, suspend)
  - Concurrency conflict detection
  - Soft-delete behavior
- Integration test skeleton (Testcontainers)

### Observability

- OpenTelemetry tracing (AspNetCore, EF Core, HTTP, runtime)
- Prometheus endpoint
- Custom RouteMetrics
- Health checks at `/health`
- Query logging (opt-in)
- Runtime crash diagnostics

### Known Limitations

- Integration tests have health-check coverage only (no full CRUD integration tests yet)
- No Booking Service sync consumer for domain events
- No Quartz.NET scheduled jobs yet (job framework wired, no jobs scheduled)
- No load/stress test scripts committed yet
