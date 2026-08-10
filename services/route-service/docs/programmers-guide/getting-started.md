# Programmer's Guide — Getting Started

## Prerequisites
- .NET 10 SDK
- Docker (for Postgres, Redis, RabbitMQ)
- EF Core CLI (`dotnet tool install --global dotnet-ef`)

## Setup
1. Clone the repo
2. `cd services/route-service`
3. `dotnet restore`
4. Ensure `appsettings.json` has correct connection strings (or set env vars)
5. `dotnet ef migrations add InitialCreate --project src/RouteService.Infrastructure --startup-project src/RouteService.Api`
6. `dotnet build`
7. `dotnet run --project src/RouteService.Api`

## Running Tests
- Unit: `dotnet test tests/RouteService.UnitTests`
- Integration: `dotnet test tests/RouteService.IntegrationTests` (requires Docker)
- Load (k6): `k6 run tests/load/k6/route-service-load.js`
- Load (NBomber): `dotnet run --project tests/load/nbomber/RouteServiceLoadTests.csproj`

## Project Structure
```
src/
  RouteService.Domain/          # Entities, enums, events, exceptions
  RouteService.Application/     # CQRS handlers, validators, DTOs, Result pattern
  RouteService.Infrastructure/  # EF Core, outbox, messaging, caching, resilience
  RouteService.Api/             # REST endpoints, gRPC, middleware, DI
tests/
  RouteService.UnitTests/       # xUnit + InMemory
  RouteService.IntegrationTests/# Testcontainers
  load/                         # k6, NBomber, JMeter
docs/
  architecture.md
  db-schema.md
  roadmap.md
  ...
```

## Conventions
- Feature folders under `Application/Features/<Aggregate>/<UseCase>/`
- One handler per use case (vertical slice)
- All mutations publish domain events
- All public API endpoints documented via OpenAPI/Scalar
- Validation via FluentValidation
- Errors returned via `Result<T>` pattern
