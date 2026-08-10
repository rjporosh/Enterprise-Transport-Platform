# Route Service — Roadmap

## v1.0.0 (Current)
- [x] Route CRUD with soft delete
- [x] Stop CRUD with soft delete
- [x] Schedule CRUD with soft delete
- [x] Optimistic concurrency
- [x] Audit logging
- [x] Pagination, filtering, search
- [x] Bangla/English localization
- [x] REST + gRPC
- [x] RabbitMQ transactional outbox
- [x] Serilog + OpenTelemetry
- [x] Health checks
- [x] Rate limiting
- [x] Release info endpoint
- [x] Unit + integration test skeletons
- [x] Performance test templates (k6, NBomber, JMeter)

## v1.1.0
- [ ] Route stop ordering editor
- [ ] Fare zone integration
- [ ] Booking Service sync consumer
- [ ] Plate-number / operator-transfer endpoint (for future bus-route assignment)
- [ ] Full-text search (Postgres `pg_trgm` / SQL Server full-text)

## v1.2.0
- [ ] Real-time route status webhooks
- [ ] GeoJSON export for stops
- [ ] Schedule conflict detection (overlapping departures on same route)
- [ ] CQRS read-model projection for high-volume queries

## v2.0.0
- [ ] GraphQL API
- [ ] Multi-tenant schema isolation
- [ ] Offline-first mobile sync protocol
