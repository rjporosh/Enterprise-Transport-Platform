# Payment Service - Programmer Guide

## Project Structure

```
services/payment-service/
├── PaymentService.sln
├── src/
│   ├── PaymentService.Domain/         # Entities, value objects, domain events, exceptions
│   ├── PaymentService.Application/   # CQRS, validators, DTOs, interfaces, behaviors
│   ├── PaymentService.Infrastructure/ # EF Core, RabbitMQ, Redis, metrics, providers
│   └── PaymentService.Api/            # Minimal API endpoints, middleware, auth
├── tests/
│   ├── PaymentService.UnitTests/
│   └── PaymentService.IntegrationTests/
├── performance-tests/
│   ├── k6/
│   ├── jmeter/
│   └── nbomber/
└── docs/programmers-guide/
```

## Creating a New Payment Feature

1. Create a folder under `Features/Payments/{FeatureName}/`
2. Add `{FeatureName}Command.cs` or `{FeatureName}Query.cs`
3. Add `{FeatureName}Handler.cs`
4. Add `{FeatureName}Validator.cs` (if input validation needed)
5. Register in `PaymentEndpoints.cs`
6. Add domain events if state changes

## Database Migrations

```bash
dotnet ef migrations add InitialCreate --project src/PaymentService.Infrastructure --startup-project src/PaymentService.Api
dotnet ef database update --project src/PaymentService.Infrastructure --startup-project src/PaymentService.Api
```

## Adding a Payment Provider

1. Create a new class implementing `IPaymentProvider`
2. Register in `PaymentProviderFactory`
3. Configure via `Payment:Provider` appsetting

## Event Catalog

| Event | Routing Key | Consumer |
|-------|-------------|----------|
| PaymentCreatedDomainEvent | payment.created | Notification Service |
| PaymentProcessingDomainEvent | payment.processing | Internal |
| PaymentSucceededDomainEvent | payment.succeeded | Notification, Booking |
| PaymentFailedDomainEvent | payment.failed | Notification |
| PaymentCancelledDomainEvent | payment.cancelled | Notification |
| PaymentRefundedDomainEvent | payment.refunded | Notification, Booking |

## Debugging

1. Check `logs/runtime-error-logs/` for startup/dependency failures
2. Check `logs/exception-logs/` for application exceptions
3. Check `logs/query-logs/` for slow database queries
4. Use CorrelationId from response header to trace across services
5. OpenTelemetry traces available at Jaeger (`http://localhost:16686`)
