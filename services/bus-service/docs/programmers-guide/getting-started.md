# Programmer's Guide — Getting Started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/products/docker-desktop/) + Docker Compose
- [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) tool: `dotnet tool install --global dotnet-ef`

## Quick Start

```bash
# 1. Start dependencies
docker compose -f infrastructure/docker/docker-compose.yml up -d postgres-bus redis rabbitmq

# 2. Apply migrations (Development auto-migrates on startup, but you can run manually)
cd services/bus-service/src/BusService.Infrastructure
dotnet ef database update --startup-project ../BusService.Api

# 3. Run the API
cd ../BusService.Api
dotnet run
# → http://localhost:5201/scalar
```

## Project Structure

```
BusService.Api/              ← Entry point, middleware, DI, health checks
BusService.Application/      ← CQRS handlers, validators, DTOs, interfaces
BusService.Infrastructure/   ← EF Core, Redis, RabbitMQ, outbox, logging
BusService.Domain/           ← Entities, events, exceptions, enums
tests/
  BusService.UnitTests/      ← Domain + handler tests (NSubstitute, FluentAssertions)
  BusService.IntegrationTests/ ← WebApplicationFactory + Testcontainers
docs/
  db-schema.md               ← Tables, indexes, constraints
  diagrams/c4/               ← C4 models
  scripts/postman/           ← Postman collection
  programmers-guide/         ← This folder
  testing/                   ← Test strategy docs
```

## Adding a New Feature

1. **Domain**: Add/modify entity in `Domain/Entities/`, raise domain events.
2. **Application**: Create `Features/<Feature>/<CommandOrQuery>.cs`, `<Handler>.cs`, `<Validator>.cs`.
3. **Infrastructure**: Add DbContext configuration, repository if needed.
4. **API**: Register endpoint in `Api/Endpoints/BusEndpoints.cs`.
5. **Migration**: `dotnet ef migrations add <Name> --startup-project src/BusService.Api`.
6. **Tests**: Add unit tests in `tests/BusService.UnitTests/<Feature>/`.

## Conventions

- **CQRS**: Commands mutate state, queries read state. One handler per command/query.
- **Validation**: FluentValidation validators in the same folder as the handler.
- **Error handling**: Throw domain exceptions; `ExceptionHandlingMiddleware` maps them to structured JSON.
- **Auditing**: `IAuditLogger` is injected into handlers; log user, action, entity, and changes.
- **Events**: Domain events are raised in the aggregate; `OutboxEventPublisher` persists them atomically with the state change.
