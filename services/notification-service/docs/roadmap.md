# Notification Service — Roadmap

## v1.0.0 (Current)

- Email/SMS/Push delivery with provider abstraction
- Templates with Scriban rendering and locale fallback
- Recipient preferences with channel opt-out
- Quartz scheduling and automatic retry
- Outbox pattern with RabbitMQ
- Upstream event consumption (Auth/Booking/Payment)
- REST + gRPC APIs
- Health checks, OpenTelemetry, Prometheus metrics
- Multi-provider database abstraction (Postgres, SqlServer, MySql)

## v1.1.0 (Planned)

- Auth Service `GET /api/v1/users/{id}/contact` endpoint for Booking/Payment recipient resolution
- Redis-backed idempotency cache
- In-app notification channel
- Batch send/cancel/retry endpoints
- Template versioning UI and rollback
- Provider reconciliation job (FCM/SMS delivery status sync)
- Delivery receipt webhooks (provider DLR → MarkDelivered)

## v2.0.0 (Future)

- MongoDB document provider (separate persistence implementation)
- Oracle EF Core provider (pending license review)
- Advanced analytics dashboard
- On-call alerting integration
- Webhook-based event delivery to downstream consumers
