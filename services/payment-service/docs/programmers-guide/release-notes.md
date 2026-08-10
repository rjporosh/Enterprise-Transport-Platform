# Payment Service - Release Notes

## v1.2.0 - Phase 2: Nagad + Stripe + Webhook Signature Verification

### Features

- Real Nagad tokenized payment provider (sandbox-ready)
- Real Stripe PaymentIntent provider for card processing (MasterCard/Visa)
- Webhook signature verification for all providers:
  - bKash: HMAC-SHA256 via `X-Bkash-Signature`
  - Nagad: HMAC-SHA256 via `X-Nagad-Signature`
  - Stripe: `Stripe-Signature` header with timestamp tolerance
- PaymentProviderFactory resolves Nagad and Stripe from DI
- Named HttpClients registered for Nagad and Stripe

### Database

- No schema changes

### Testing

- 8 new unit tests for webhook signature verification (bKash, Nagad, Stripe)
- Total: 32 passing unit tests

### Known Limitations

- Scheduled reconciliation jobs require Quartz.NET integration
- Card processing requires Stripe account and webhook endpoint configuration

## v1.1.0 - Phase 1: Agent Payment Methods + bKash Provider

### Features

- Agent/Merchant/Personal payment method management endpoints
- AgentPaymentMethod entity with domain validation
- Real bKash tokenized checkout provider (sandbox-ready)
- Polly retry (3 attempts, exponential backoff), timeout (30s), and circuit breaker (5 failures / 30s)
- Correlation ID propagation from HTTP requests to external provider calls
- Idempotency enforced at create and process levels
- Payment status saved to DB before calling external provider (prevents double-charge on crash)

### Database

- New `agent_payment_methods` table with unique constraint on `(AgentId, Provider, AccountNumber)`
- Migration: `20260810195845_AddAgentPaymentMethod`

### Testing

- 9 new unit tests for AgentPaymentMethod domain rules
- Total: 24 passing unit tests

### Known Limitations

- Nagad provider not yet implemented (coming in Phase 2)
- Card processing via Stripe not yet implemented (coming in Phase 2)
- bKash webhook signature verification uses framework (not yet full HMAC validation)
- Scheduled reconciliation jobs require Quartz.NET integration

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
