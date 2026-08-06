# Bus Service

Fleet management — the canonical source of truth for buses and depots
across the Enterprise Transport Platform. Built with .NET 10, Clean
Architecture, and CQRS (MediatR), matching Auth and Booking Service's
conventions. See
[`docs/architecture/bus-service-architecture.md`](../../docs/architecture/bus-service-architecture.md)
for design rationale, and that doc plus
[`scripts/README.md`](../../scripts/README.md) for the file-based
diagnostic logging feature.

> **Build status note**: written without a local .NET SDK or network
> access — carefully hand-reviewed (brace-balanced, cross-checked against
> the exact bug classes already found in Auth/Booking Service this
> session: `IMeterFactory`'s namespace, the `AspNetCore.HealthChecks.Rabbitmq`
> 9.0.0 API break, native-OpenAPI-not-Swashbuckle, `DomainEvents`/`Version`
> `Ignore()`d from the start) but **not actually compiled**. Run a real
> `dotnet build` (or `scripts/dotnet-build.sh services/bus-service/BusService.sln`)
> to confirm before deploying.

## What's here

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `BusService.Domain` | `Bus` (aggregate root, lifecycle rules), `Depot` |
| Application | `BusService.Application` | RegisterBus, GetBus, GetBuses, UpdateBusDetails, ChangeBusStatus, CreateDepot, GetDepots |
| Infrastructure | `BusService.Infrastructure` | EF Core (Postgres/SqlServer/MySQL switch), RabbitMQ outbox, Redis, query-log interceptor |
| Api | `BusService.Api` | Minimal API endpoints, JWT bearer auth, native OpenAPI/Scalar, health checks, OpenTelemetry, the runtime-error crash handler |
| Tests | `BusService.UnitTests`, `BusService.IntegrationTests` | Domain + handler unit tests, Testcontainers-based API tests |

## Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/buses` | Bearer, Operator/Admin | Register a new bus |
| GET | `/api/v1/buses/{id}` | Bearer | Get a bus by id (cached) |
| GET | `/api/v1/buses` | Bearer | Search — filterable by operator/depot/status, paginated |
| PUT | `/api/v1/buses/{id}` | Bearer, Operator/Admin | Update type/seats/depot/fleet details |
| POST | `/api/v1/buses/{id}/status` | Bearer, Operator/Admin | Transition status |
| POST | `/api/v1/depots` | Bearer, Admin | Create a depot |
| GET | `/api/v1/depots` | Bearer | List depots, optionally by city |
| GET | `/health` | — | Postgres (or active provider), Redis, RabbitMQ |
| GET | `/metrics` | — | Prometheus scrape endpoint |
| GET | `/scalar` | — (Development only) | Interactive API docs |

All endpoints require a JWT issued by **Auth Service** — same
`Jwt:Issuer`/`Audience`/`SigningKey` configuration, so a token from Auth
Service's `/login` validates here without any extra setup.

## Running locally

```bash
# 1. Start dependencies (from repo root)
docker compose -f infrastructure/docker/docker-compose.yml up -d postgres redis rabbitmq

# 2. Generate a migration (provider-specific — not pre-committed, see
#    docs/architecture/auth-service-er-diagram.md for why)
dotnet tool install --global dotnet-ef   # one-time, if you don't have it
cd services/bus-service/src/BusService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../BusService.Api --context BusDbContext

# 3. Run the API — applies the migration automatically in Development.
#    Recommended: the wrapper script (see ../../scripts/README.md), which
#    also captures a startup crash to logs/ automatically:
cd ../BusService.Api
../../../../scripts/dotnet-run.sh .
# or plain: dotnet run
# → http://localhost:5201/scalar
```

**On `--urls`**: include the host — `--urls=http://localhost:5201`, not
`--urls=http://5201` (the latter fails with a confusing
`SocketException: Can't assign requested address`).

## Running tests

```bash
cd services/bus-service
dotnet test tests/BusService.UnitTests
dotnet test tests/BusService.IntegrationTests   # needs Docker (Testcontainers)
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

## Further reading

- [Architecture](../../docs/architecture/bus-service-architecture.md)
- [Diagnostic logging](../../scripts/README.md)
- Auth Service's [architecture doc](../../docs/architecture/auth-service-architecture.md) §1, §8 — Clean Architecture rationale and the full database-portability write-up this service follows
