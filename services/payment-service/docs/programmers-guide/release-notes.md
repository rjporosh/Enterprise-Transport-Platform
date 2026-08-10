# Payment Service - Release Notes

## v1.0.0 - Initial Release

### Features

- Payment intent creation with idempotency key support
- Payment processing (Process, Confirm, Fail, Cancel)
- Full and partial refund support
- Payment search with pagination and filtering
- Webhook processing framework
- Transactional outbox pattern for reliable event publishing
- RabbitMQ topic exchange (`payment.events`)
- Payment state machine with strict transition validation
- Money value object with currency-safe arithmetic
- Tenant/company/organization isolation
- JWT Bearer authentication
- Rate limiting
- OpenTelemetry distributed tracing
- Prometheus metrics
- Health checks (PostgreSQL, RabbitMQ, Redis)
- Structured logging (Serilog)
- Exception diagnostics with file-based logging
- Query logging with EF Core interceptors

### Database

- PostgreSQL primary with multi-provider support
- EF Core migrations ready
- Optimistic concurrency via unique IdempotencyKey index

### Testing

- Unit tests for domain rules and handlers
- Integration tests with Testcontainers
- Load tests: k6, JMeter, NBomber

### Observability

- OpenTelemetry tracing (AspNetCore, EF Core, HTTP)
- Prometheus metrics endpoint
- Custom business metrics (payment counts, latency, refunds)
- Health checks at `/health/live` and `/health/ready`

### Known Limitations

- Default payment provider is a stub (real provider integration requires external credentials)
- Webhook signature verification is framework-ready (provider-specific implementation needed)
- Scheduled reconciliation jobs require Quartz.NET integration
