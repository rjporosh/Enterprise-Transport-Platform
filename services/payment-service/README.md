# Payment Service

Enterprise-grade payment transaction lifecycle management for the Enterprise Transport Platform.

## Features

- Payment intent creation with idempotency
- Payment processing and confirmation
- Payment failure handling
- Full and partial refund support
- Webhook processing with signature verification
- Transactional outbox pattern for reliable event publishing
- RabbitMQ event-driven architecture
- Optimistic concurrency control
- Tenant/company/organization isolation
- Rate limiting
- OpenTelemetry observability
- Health checks
- Structured logging with exception diagnostics
- Query logging
- Agent/merchant/personal payment method management (bKash, Nagad, bank accounts)
- Real bKash payment provider integration (sandbox ready)
- Real Nagad payment provider integration (sandbox ready)
- Real Stripe card payment provider (MasterCard/Visa)
- Polly retry, timeout, and circuit breaker for provider resilience
- Correlation ID propagation across HTTP and provider calls
- Webhook signature verification (bKash HMAC-SHA256, Nagad HMAC-SHA256, Stripe Stripe-Signature)

## Technology Stack

- .NET 10 / ASP.NET Core
- MediatR (CQRS)
- FluentValidation
- EF Core (PostgreSQL/SQL Server/MySQL)
- RabbitMQ
- Redis
- Serilog
- OpenTelemetry + Prometheus
- xUnit

## Quick Start

```bash
cd services/payment-service
dotnet restore
dotnet build
dotnet run --project src/PaymentService.Api
```

## API Documentation

- OpenAPI: `http://localhost:5003/openapi/v1.json`
- Scalar: `http://localhost:5003/scalar`

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `Database:Provider` | PostgreSQL | Database provider |
| `ConnectionStrings:DefaultConnection` | | Database connection string |
| `Redis:ConnectionString` | localhost:6379 | Redis connection string |
| `RabbitMQ:HostName` | localhost | RabbitMQ host |
| `Jwt:Authority` | http://localhost:5001 | Auth service URL |
| `Jwt:Audience` | payment-service | JWT audience |
| `Bkash:AppKey` | | bKash merchant app key |
| `Bkash:AppSecret` | | bKash merchant app secret |
| `Bkash:Username` | | bKash merchant username |
| `Bkash:Password` | | bKash merchant password |
| `Bkash:BaseUrl` | https://tokenized.sandbox.bka.sh/v1.2.0-beta | bKash API base URL |
| `Bkash:CallbackUrl` | | Webhook callback URL for bKash |
| `Bkash:WebhookSecret` | | Webhook signature secret |

## Testing

```bash
dotnet test services/payment-service/tests/PaymentService.UnitTests
dotnet test services/payment-service/tests/PaymentService.IntegrationTests
```

## Docker

```bash
docker build -t payment-service services/payment-service/src/PaymentService.Api
docker run -p 5003:8080 payment-service
```
