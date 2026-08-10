# Payment Service - Architecture

## 1. Overview

Payment Service owns the complete payment transaction lifecycle for the Enterprise Transport Platform. It provides a clean, auditable, and idempotent payment processing capability that can be reused across bus ticketing, e-commerce, SaaS, and other enterprise domains.

## 2. Clean Architecture

```
PaymentService.Api
PaymentService.Infrastructure
PaymentService.Application
PaymentService.Domain
```

Dependency flow: `Api → Infrastructure → Application → Domain`

Domain has zero runtime dependencies beyond `MediatR.Contracts`.

## 3. Domain Model

### Aggregates

- **Payment**: Aggregate root. Owns the complete payment lifecycle from creation through refund.
- **PaymentRefund**: Entity owned by Payment. Managed within the Payment aggregate boundary.

### Value Objects

- **Money**: Immutable value object with currency, amount, arithmetic operators, and comparison methods. Never uses floating-point.

### State Machine

```
Pending → Processing → Succeeded → PartiallyRefunded → Refunded
    ↓         ↓            ↓
  Failed    Failed      Failed
    ↓         ↓
  Cancelled  Cancelled
```

Invalid transitions throw `InvalidPaymentStateTransitionException`.

### Domain Events

- `PaymentCreatedDomainEvent`
- `PaymentProcessingDomainEvent`
- `PaymentSucceededDomainEvent`
- `PaymentFailedDomainEvent`
- `PaymentCancelledDomainEvent`
- `PaymentRefundedDomainEvent`

## 4. CQRS

All operations use MediatR:

- Commands: `CreatePayment`, `ProcessPayment`, `ConfirmPayment`, `FailPayment`, `CancelPayment`, `RefundPayment`
- Queries: `GetPaymentById`, `GetPayments`, `SearchPayments`
- Webhook: `ProcessWebhook`

## 5. Transactional Outbox

Financial events use the outbox pattern:
1. Domain event raised in aggregate
2. Serialized to `OutboxMessage` in same DbContext/SaveChanges
3. `OutboxProcessor` (BackgroundService) publishes to RabbitMQ

Routing keys: `payment.created`, `payment.processing`, `payment.succeeded`, `payment.failed`, `payment.cancelled`, `payment.refunded`

## 6. Idempotency

All state-changing operations accept `Idempotency-Key`. Duplicate requests return the original result without side effects.

## 7. Provider Abstraction

`IPaymentProvider` abstraction allows runtime provider selection via `IPaymentProviderFactory`. Current implementation includes `DefaultPaymentProvider`. Additional providers can be added without modifying business logic.

## 8. Resilience

- Polly-based circuit breaker for external providers
- Timeout configuration for all I/O
- Exponential backoff with jitter
- Unknown provider state handling (never blind-retry after timeout)

## 9. Observability

- OpenTelemetry distributed tracing
- Prometheus metrics (payment counts, latency, circuit breaker events)
- Serilog structured logging
- Query logging via EF Core interceptors
- Runtime error diagnostics
- Health checks (PostgreSQL, RabbitMQ, Redis)

## 10. Security

- JWT Bearer authentication
- Tenant/company/organization isolation
- No sensitive data in logs
- Rate limiting per endpoint
- Webhook signature verification (framework ready)

## 11. Database

- PostgreSQL primary
- SQL Server, MySQL also supported via configuration switch
- Optimistic concurrency
- Unique index on `IdempotencyKey`
- Outbox table for reliable event publishing

## 12. Testing

- Unit tests: domain rules, handlers, validators
- Integration tests: API with Testcontainers
- Performance tests: k6, JMeter, NBomber
