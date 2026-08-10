# Bus Service

Fleet management — the canonical source of truth for buses and depots
across the Enterprise Transport Platform. Built with .NET 10, Clean
Architecture, and CQRS (MediatR), matching Auth and Booking Service's
conventions.

## What's here

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `BusService.Domain` | `Bus` (aggregate root, lifecycle rules), `Depot` |
| Application | `BusService.Application` | RegisterBus, GetBus, GetBuses, UpdateBusDetails, ChangeBusStatus, CreateDepot, GetDepots |
| Infrastructure | `BusService.Infrastructure` | EF Core (Postgres/SqlServer/MySQL switch), RabbitMQ outbox, Redis, query-log interceptor, audit logging |
| Api | `BusService.Api` | Minimal API endpoints, JWT bearer auth, native OpenAPI/Scalar, health checks, OpenTelemetry, runtime-error crash handler |
| Tests | `BusService.UnitTests`, `BusService.IntegrationTests` | Domain + handler unit tests, Testcontainers-based API tests |

## Documentation

- **[Architecture](../../docs/architecture/bus-service-architecture.md)** — Design rationale, bus lifecycle, database portability, file-based logging.
- **[Database Schema](docs/db-schema.md)** — Tables, indexes, constraints, migrations.
- **[C4 Diagrams](docs/diagrams/c4/)** — Context, Container, Component, Deployment, Code diagrams (Mermaid).
- **[Postman Collection](docs/scripts/postman/bus-service.postman-collection.json)** — Ready-to-import collection with example requests.
- **[Programmer's Guide](docs/programmers-guide/)** — Getting started, API contracts, CQRS, validation, gRPC, consuming events, repository pattern, migrations, testing, background workers, adding entities, folder structure.
- **[Testing](docs/testing/)** — Unit, integration, functional, performance test strategies and test cases.

## Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/v1/buses` | Bearer, Operator/Admin | Register a new bus |
| `GET` | `/api/v1/buses/{id}` | Bearer | Get a bus by id (cached) |
| `GET` | `/api/v1/buses` | Bearer | Search — filterable by operator/depot/status, paginated |
| `PUT` | `/api/v1/buses/{id}` | Bearer, Operator/Admin | Update type/seats/depot/fleet details |
| `POST` | `/api/v1/buses/{id}/status` | Bearer, Operator/Admin | Transition status |
| `DELETE` | `/api/v1/buses/{id}` | Bearer, Operator/Admin | Soft delete a bus |
| `POST` | `/api/v1/buses/{id}/restore` | Bearer, Operator/Admin | Restore a soft-deleted bus |
| `POST` | `/api/v1/depots` | Bearer, Admin | Create a depot |
| `GET` | `/api/v1/depots` | Bearer | List depots, optionally by city |
| `DELETE` | `/api/v1/depots/{id}` | Bearer, Admin | Soft delete a depot |
| `POST` | `/api/v1/depots/{id}/restore` | Bearer, Admin | Restore a depot |
| `GET` | `/health` | — | Postgres (or active provider), Redis, RabbitMQ |
| `GET` | `/metrics` | — | Prometheus scrape endpoint |
| `GET` | `/scalar` | — (Development only) | Interactive API docs |

All endpoints require a JWT issued by **Auth Service** — same
`Jwt:Issuer`/`Audience`/`SigningKey` configuration, so a token from Auth
Service's `/login` validates here without any extra setup.

## Running locally

```bash
# 1. Start dependencies (from repo root)
docker compose -f infrastructure/docker/docker-compose.yml up -d postgres-bus redis rabbitmq

# 2. Generate a migration (provider-specific — not pre-committed, see
#    docs/architecture/bus-service-architecture.md for why)
dotnet tool install --global dotnet-ef   # one-time, if you don't have it
cd services/bus-service/src/BusService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../BusService.Api --context BusDbContext

# 3. Run the API — applies the migration automatically in Development.
cd ../BusService.Api
dotnet run
# → http://localhost:5201/scalar
```

## Running tests

```bash
cd services/bus-service
dotnet test tests/BusService.UnitTests
dotnet test tests/BusService.IntegrationTests   # needs Docker (Testcontainers)
dotnet test tests/functional                    # needs running Docker Compose stack
k6 run tests/load/k6/bus-service-load-test.js   # needs k6 installed
```

## Configuration

| Key | Default | Notes |
|---|---|---|
| `Database:Provider` | `Postgres` | `Postgres` \| `SqlServer` \| `MySql` |
| `ConnectionStrings:BusDb` | local Postgres | Format depends on the selected provider |
| `Jwt:SigningKey` / `Issuer` / `Audience` | placeholder / Auth Service's defaults | **Must match Auth Service's config exactly** — override the placeholder key in every real environment |
| `Redis:ConnectionString` | `localhost:6379` | |
| `RabbitMq:HostName` | `localhost` | |
| `Logging:FileLogsDirectory` | `../../logs` (resolves to `services/bus-service/logs` under `dotnet run`) | Override with an absolute path for a published/deployed build |
| `Logging:EnableQueryLogging` | `false` (`true` in Development) | See `scripts/README.md`, "Query log" |

## Docker

```bash
# Build and run with Docker Compose
docker compose -f infrastructure/docker/docker-compose.yml up bus-service

# Or build just this service
docker build -t bus-service services/bus-service/src/BusService.Api
```

## SQL Scripts

See `docs/scripts/sql/` for:
- `schema.sql` — DDL for all tables, indexes, constraints
- `stored-procedures.sql` — Stored procedures for common queries
- `functions.sql` — Scalar and table-valued functions
- `views.sql` — Reporting views (active fleet, depot utilization, audit summary)
