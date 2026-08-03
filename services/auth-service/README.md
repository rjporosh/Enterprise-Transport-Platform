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
| GET | `/scalar` | — (Development only) | Interactive API docs (native OpenAPI + Scalar — see note below) |

> **API docs note**: this service uses ASP.NET Core's native OpenAPI
> generation (`Microsoft.AspNetCore.OpenApi`) + Scalar, not Swashbuckle —
> deliberately. Swashbuckle and the native generator disagree on the
> `OpenApiDocument` shape on .NET 10; running both, or pointing Scalar at
> Swashbuckle's `/swagger/v1/swagger.json` route instead of the native
> `/openapi/v1.json`, is why an earlier version of this service showed an
> empty Scalar page with no endpoints listed. See
> `docs/architecture/auth-service-architecture.md` §13 for the full story.

## Running locally

**A migration is already committed** — `InitialCreate`, under
`src/AuthService.Infrastructure/Migrations/`. You do **not** need to
generate one yourself unless you've changed the entity model since. See
`docs/architecture/auth-service-er-diagram.md` for why migrations are
provider-specific (this one targets Postgres — regenerate for SqlServer/MySQL
if you switch `Database:Provider`).

```bash
# 1. Start dependencies (from repo root)
docker compose -f infrastructure/docker/docker-compose.yml up -d postgres redis rabbitmq

# 2. Run the API — applies the committed migration automatically in
#    Development (see Program.cs). Use the wrapper script (recommended —
#    see scripts/README.md) so a startup crash is saved to logs/ automatically.
#    Run this from services/auth-service/:
../../scripts/dotnet-run.sh src/AuthService.Api
# or plain: cd src/AuthService.Api && dotnet run
# → http://localhost:5101/scalar
```

**If you changed the entity model** and need a new migration:

```bash
dotnet tool install --global dotnet-ef   # one-time, if you don't have it
cd src/AuthService.Infrastructure
dotnet ef migrations add <DescriptiveName> --startup-project ../AuthService.Api --context AuthDbContext
```

**On `--urls`**: pass a full URL including the host, e.g.
`--urls=http://localhost:5012` — `--urls=http://5012` (missing the host)
is not a shorthand for "port 5012" and fails with a confusing
`SocketException: Can't assign requested address`, since Kestrel tries to
bind to a host literally named `5012`.

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
