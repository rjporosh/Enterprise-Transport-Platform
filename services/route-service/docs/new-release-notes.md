# Release Notes — Route Service v1.0.0

**Release Date**: 2026-08-10  
**Build**: local  
**Commit**: unknown

## New Features
- Route, Stop, and Schedule CRUD with soft delete and restore
- Optimistic concurrency via `Version` token
- Pagination, filtering, and search across routes and stops
- Bangla and English localization support
- REST API with Scalar documentation (`/scalar`)
- gRPC service for `GetRoute` and `SearchRoutes`
- RabbitMQ transactional outbox publishing domain events
- Audit logging to `audit_logs` table and Serilog
- Serilog structured logging with correlation IDs
- OpenTelemetry tracing and Prometheus metrics
- Health checks for Postgres, Redis, RabbitMQ
- Rate limiting on write endpoints
- Release information endpoint (`GET /api/v1/release/info`) for SQA/testers
- Unit tests (xUnit + InMemory) and integration test skeleton (Testcontainers)
- Performance test templates: k6, NBomber, JMeter

## Breaking Changes
- None (initial release)

## Known Issues
- EF Core migrations are not generated; run `dotnet ef migrations add InitialCreate`
- Resilience policies are not configurable via appsettings yet
- Audit log IP address population is not implemented

## Upgrade Guide
- N/A (first release)
