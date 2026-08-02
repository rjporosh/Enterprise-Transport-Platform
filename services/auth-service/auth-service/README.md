# Auth Service

Identity, authentication, and account-security audit trail for the
Enterprise Transport Platform. Built with .NET 10, Clean Architecture, and
CQRS (MediatR) — see [`docs/architecture/auth-service-architecture.md`](../../docs/architecture/auth-service-architecture.md)
for the full design rationale.

> **Build status note (updated after a real `dotnet build` pass)**: this
> was originally built in a sandbox with no .NET SDK and no network access
> — hand-reviewed but not compiled. A real `dotnet build` against it then
> surfaced 3 compile errors and several NuGet warnings; all are now fixed —
> see the git log (`git log --oneline`) for a fix-by-fix breakdown, and
> `docs/architecture/auth-service-architecture.md` §13 for the technical
> detail on each. That fix pass was itself done without a local .NET SDK
> or network access, verified instead against the `project.assets.json`
> files left behind in this repo's `obj/` folders by the failed build (the
> actual resolved dependency graph, not a guess) plus targeted research for
> correct current package versions. **Still recommended**: run a clean
> `dotnet build` yourself to confirm — this pass fixed everything that was
> reported plus a few more issues the same errors were masking, but a
> second pair of eyes (and a real compiler) on any diff this size is always
> worth it.

## What's here

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `AuthService.Domain` | `User`, `Role`, `RefreshToken`, `AuditLog` — zero framework deps |
| Application | `AuthService.Application` | CQRS commands/queries: Register, Login, RefreshToken, Logout, ChangePassword, GetCurrentUser, GetAuditLogs |
| Infrastructure | `AuthService.Infrastructure` | EF Core (Postgres/SqlServer/MySQL switch), JWT, PBKDF2, Redis, RabbitMQ outbox |
| Api | `AuthService.Api` | Minimal API endpoints, JWT bearer auth, rate limiting, Swagger/Scalar, health checks, OpenTelemetry |
| Tests | `AuthService.UnitTests`, `AuthService.IntegrationTests` | Handler unit tests, Testcontainers-based API tests |
| Load tests | `tests/load/{k6,jmeter,nbomber}` | Login load, register-race/stress — 3 tools, same scenarios |

## Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/auth/register` | — | Create an account, returns token pair immediately |
| POST | `/api/v1/auth/login` | — | Sign in, returns token pair |
| POST | `/api/v1/auth/refresh` | — (refresh token in body) | Rotate refresh token, returns new pair |
| POST | `/api/v1/auth/logout` | — (refresh token in body) | Revoke a refresh token |
| GET | `/api/v1/auth/me` | Bearer | Signed-in user's profile |
| POST | `/api/v1/auth/change-password` | Bearer | Change password (requires current password) |
| GET | `/api/v1/auth/audit-logs` | Bearer, Admin role | Search the security audit trail |
| GET | `/health` | — | Liveness/readiness (DB, Redis, RabbitMQ) |
| GET | `/metrics` | — | Prometheus scrape endpoint |
| GET | `/scalar/v1` | — (Development only) | Interactive API docs |

## Running locally

**Migrations are not optional** — skip step 2 and the app starts fine
(no crash) but every table is missing, so `/register` etc. will fail with
a SQL "relation does not exist" error the first time anything touches the
database. See `docs/architecture/auth-service-er-diagram.md` for why
migrations aren't pre-committed to this repo (they're provider-specific).

```bash
# 0. One-time: install the EF Core CLI tool if you don't have it
dotnet tool install --global dotnet-ef

# 1. Start dependencies (from repo root)
docker compose -f infrastructure/docker/docker-compose.yml up -d postgres redis rabbitmq

# 2. Generate migrations (REQUIRED — see note above)
cd services/auth-service/src/AuthService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../AuthService.Api --context AuthDbContext

# 3. Run the API (auto-applies pending migrations in Development — see Program.cs)
cd ../AuthService.Api
dotnet run
# → http://localhost:5101/scalar/v1
```

## Running tests

```bash
cd services/auth-service
dotnet test tests/AuthService.UnitTests
dotnet test tests/AuthService.IntegrationTests   # needs Docker (Testcontainers)
```

See [`tests/load/README.md`](tests/load/README.md) for k6/JMeter/NBomber.

## Configuration

All config lives in `src/AuthService.Api/appsettings.json`, overridable via
environment variables (`Jwt__SigningKey`, `ConnectionStrings__AuthDb`,
`Database__Provider`, etc.) or `dotnet user-secrets` locally. **The default
`Jwt:SigningKey` is a placeholder — it must be overridden with a real
secret (32+ chars) before this touches anything but a laptop.**

| Key | Default | Notes |
|---|---|---|
| `Database:Provider` | `Postgres` | `Postgres` \| `SqlServer` \| `MySql` — see architecture doc §8 |
| `ConnectionStrings:AuthDb` | local Postgres | Format depends on the selected provider |
| `Jwt:SigningKey` | placeholder | **Override in every real environment** |
| `Jwt:AccessTokenLifetimeMinutes` | 15 | |
| `Jwt:RefreshTokenLifetimeDays` | 30 | |
| `Redis:ConnectionString` | `localhost:6379` | |
| `RabbitMq:HostName` | `localhost` | |

## Further reading

- [Architecture](../../docs/architecture/auth-service-architecture.md) — design rationale, known gaps
- [C4 diagrams](../../docs/architecture/auth-service-c4-diagrams.md) — context/container/component + sequence diagrams
- [ER diagram & table design](../../docs/architecture/auth-service-er-diagram.md)
- [Delivery plan](../../docs/architecture/auth-service-plan.md) — what's done, what's not
- [How to add a new CRUD endpoint](../../docs/development/how-to-add-a-new-crud-endpoint.md)
- [Postman collection](../../postman/README.md)
