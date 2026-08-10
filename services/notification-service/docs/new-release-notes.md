# Notification Service — Release Notes

## v1.0.0 — 2026-08-10

### New Features

- Email notification delivery via SMTP (MailKit)
- SMS notification delivery via Twilio and Generic HTTP providers
- Push notification delivery via Firebase Cloud Messaging HTTP v1
- Notification templates with Scriban server-side rendering
- Template locale fallback (requested locale → English)
- Recipient preferences with channel opt-out support
- Quartz.NET scheduled notification dispatch (every 10s)
- Stuck notification recovery job (every 5 min)
- Automatic retry with exponential backoff (capped at 60 min)
- Dead-letter queue for exhausted retries
- Outbox pattern for reliable event publishing
- RabbitMQ upstream event consumer (Auth/Booking/Payment events)
- REST API with pagination, filtering, and search
- gRPC SendNotification and GetNotificationStatus endpoints
- Result Pattern with multi-error validation responses
- Centralized exception handling (RFC 7807 ProblemDetails)
- Soft delete for notifications and templates
- Optimistic concurrency (xmin rowversion for notifications, bytea rowversion for templates)
- Serilog structured logging with daily rolling files
- OpenTelemetry distributed tracing and Prometheus metrics
- Health checks for PostgreSQL and RabbitMQ
- Multi-provider database abstraction (PostgreSQL, SQL Server, MySQL)
- Provider abstraction for email, SMS, and push channels
- Idempotency-Key middleware for write operations
- Rate limiting (60 writes/min per IP)
- CorrelationId propagation across all requests
- Localization support (English, Bangla)
- SQA release endpoint (`GET /api/v1/release`)

### API Changes

- New endpoints: POST `/api/v1/notifications`, GET `/api/v1/notifications/{id}`, GET `/api/v1/notifications`, POST `/api/v1/notifications/{id}/cancel`, POST `/api/v1/notifications/{id}/retry`, POST `/api/v1/notifications/{id}/delete`
- Template CRUD: POST/PUT/GET/DELETE `/api/v1/templates`, GET `/api/v1/templates`
- Preferences: GET/PUT `/api/v1/recipients/{recipientId}/preferences`
- gRPC: SendNotification, GetNotificationStatus
- Health: GET `/health`, GET `/metrics`, GET `/scalar` (dev only)

### Database Changes

- Initial migration: `20260807133500_InitialCreate`
- Schema: `notification.notifications`, `notification.notification_templates`, `notification.recipient_preferences`, `notification.notification_logs`, `notification.outbox_messages`

### Configuration Changes

- `Database:Provider` — selects EF Core provider
- `RabbitMq:*` — message broker configuration
- `Smtp:*`, `Sms:*`, `Push:*` — channel provider configuration
- `Retry:*` — Polly retry configuration
- `Jwt:*` — operator endpoint authentication

### Breaking Changes

None. This is the initial release.

### Known Issues

1. Auth Service `GET /api/v1/users/{id}/contact` endpoint does not exist yet
2. Idempotency cache is in-memory (breaks across replicas)
3. Oracle and MongoDB providers not wired
4. Search uses `ToLower().Contains()` (no case-insensitive index on Postgres)

### Testing Notes

- 27 unit tests pass
- 5 integration tests pass (requires Docker)
- k6, NBomber, and JMeter load tests provided
