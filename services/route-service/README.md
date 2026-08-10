# Route Service

Enterprise Transport Platform — canonical source of truth for routes, stops, and schedules.

## Stack

- **Runtime:** .NET 10
- **Architecture:** Clean Architecture + CQRS (MediatR) + FluentValidation
- **Database:** EF Core with Postgres / SqlServer / MySQL switch
- **Messaging:** RabbitMQ transactional outbox (`route.events` exchange)
- **Cache:** Redis cache-aside
- **Observability:** Serilog, OpenTelemetry, Prometheus metrics, health checks
- **API:** REST (Scalar docs) + gRPC

## Quick Start

```bash
cd services/route-service
dotnet restore
dotnet build
dotnet test tests/RouteService.UnitTests

# Generate migration (if not present)
dotnet ef migrations add InitialCreate --project src/RouteService.Infrastructure --startup-project src/RouteService.Api

# Run
dotnet run --project src/RouteService.Api
```

## Environment Variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `ConnectionStrings:RouteDb` | `Host=localhost;...` | Database connection string |
| `Database:Provider` | `Postgres` | `Postgres` \| `SqlServer` \| `MySql` |
| `Jwt:SigningKey` | dev key | JWT validation key |
| `Jwt:Issuer` | `https://identity.bus-ticketing.local` | Token issuer |
| `Jwt:Audience` | `bus-ticketing-api` | Token audience |
| `Redis:ConnectionString` | `localhost:6379` | Redis connection |
| `RabbitMq:HostName` | `localhost` | RabbitMQ host |
| `OpenTelemetry:OtlpEndpoint` | `http://localhost:4317` | OTLP endpoint |

## Endpoints

- `GET /health` — health check
- `GET /scalar` — API docs (Development)
- `GET /metrics` — Prometheus metrics
- `POST /api/v1/stops` — create stop (Admin, Operator)
- `GET /api/v1/stops` — list stops
- `PUT /api/v1/stops/{id}` — update stop
- `DELETE /api/v1/stops/{id}` — soft-delete stop
- `POST /api/v1/routes` — create route (Admin, Operator)
- `GET /api/v1/routes` — list routes
- `GET /api/v1/routes/{id}` — get route
- `PUT /api/v1/routes/{id}` — update route
- `DELETE /api/v1/routes/{id}` — soft-delete route
- `POST /api/v1/schedules` — create schedule (Admin, Operator)
- `GET /api/v1/schedules` — list schedules
- `POST /api/v1/schedules/{id}/activate` — activate
- `POST /api/v1/schedules/{id}/suspend` — suspend

## Documentation

- [Architecture](../docs/architecture/route-service-architecture.md)
- [C4 Diagrams](../docs/architecture/route-service-c4-diagrams.md)
- [ER Diagram](../docs/architecture/route-service-er-diagram.md)
- [ADRs](../docs/architecture/route-service-adr.md)
- [Programmer Guide](programmers-guide/developer-guide.md)
- [DB Schema](programmers-guide/db-schema.md)
- [Operations](programmers-guide/operations.md)
- [Postman Collection](programmers-guide/postman-collection.json)
- [Release Notes](programmers-guide/release-notes.md)

## Tests

```bash
dotnet test tests/RouteService.UnitTests
```

## License

MIT
