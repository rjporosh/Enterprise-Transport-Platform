# Enterprise Transport Platform — Root Developer Guide

This is the one place a new developer needs to look to build, run, and
**create/apply a database migration for any of the 6 backend services**
without having to go hunting through each service's own docs first. Each
service also has its own more detailed `guide.md` — start here, then drill
into a service's own guide for anything specific to it.

For the current production-readiness picture see
[`docs/PRODUCTION-GAP-ANALYSIS.md`](docs/PRODUCTION-GAP-ANALYSIS.md),
[`docs/PRODUCTION-MILESTONES.md`](docs/PRODUCTION-MILESTONES.md) and
[`docs/API-GAPS.md`](docs/API-GAPS.md). Milestone **M0** (shared kernel + API
gateway) is done — this guide reflects that.

---

# First use — from a fresh clone (step by step)

## 1. Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| .NET SDK | 10.0.x | `dotnet --version` |
| Docker + Docker Compose | any recent | `docker info`, `docker compose version` |
| Node.js | 22.22.3+ / 24.15+ / 26+ | `node --version` |
| npm | 10+ | `npm --version` |
| EF Core CLI | matches SDK | `dotnet tool install --global dotnet-ef` |

Everything below is run from the repo root:
`cd ~/Downloads/porosh/Enterprise-Transport-Platform`

## 2. Start infrastructure + the whole stack (Docker — the easy path)

```bash
cd infrastructure/docker
docker compose up -d --build
```

This builds and starts: Postgres ×6, RabbitMQ, Redis, MailHog, the 6 backend
services, the **API gateway**, and both frontends.

| Component | URL |
|-----------|-----|
| **API gateway (the only public API entry point)** | http://localhost:8088 |
| Customer web (Angular) | http://localhost:4200 |
| Admin console (React) | http://localhost:5173 |
| RabbitMQ management | http://localhost:15672  (guest / guest) |
| MailHog (captures dev email) | http://localhost:8025 |

> **Known issue (pre-existing, tracked for M11):** the `auth-service` and
> `payment-service` Dockerfiles use `useradd -u 1000`, which collides with the
> Ubuntu-based .NET 10 runtime image (uid 1000 is taken). If a service image
> fails to build with `useradd ... exit code 4`, that is the cause — the
> gateway Dockerfile already does it the right way (`USER app`). Until those
> Dockerfiles are fixed you can still run those two services with
> `dotnet run` (next section) and point the gateway at them.

## 3. Configure environment / secrets

The committed `docker-compose.yml` is a **local dev/demo** stack. It uses a
single shared dev JWT signing key (`dev-only-signing-key-change-me-32chars-minimum`)
and `POSTGRES_PASSWORD: changeme` — **never use these in production**
(distinct per-service keys + real secrets are milestone M11).

For local `dotnet run`, each service reads `appsettings.Development.json`
(committed, dev-only values). The gateway reads
`infrastructure/gateway/src/Platform.Gateway/appsettings.Development.json`.

Production config is injected via environment variables — the important ones:
- Gateway: `Jwt__SigningKey` (**required — the gateway refuses to start in
  Production without it**), `Jwt__Issuer`, `Jwt__Audience`,
  `ReverseProxy__Clusters__<name>__Destinations__primary__Address` per service.
- Services: `ConnectionStrings__<Name>Db`, `Jwt__SigningKey` (must match what
  the gateway/auth-service issue), `RabbitMq__HostName`, `Redis__ConnectionString`.

## 4. Run backend + gateway locally (without full Docker)

Bring up just the infra:

```bash
cd infrastructure/docker
docker compose up -d postgres postgres-auth postgres-bus postgres-route \
  postgres-payment postgres-notification rabbitmq redis mailhog
```

Apply migrations (see the "Applying migrations" section further down).

Run each service in its own terminal:

```bash
dotnet run --project services/auth-service/src/AuthService.Api          # :5101
dotnet run --project services/booking-service/src/BookingService.Api    # see its launchSettings
dotnet run --project services/bus-service/src/BusService.Api            # :5201
dotnet run --project services/route-service/src/RouteService.Api        # :5401
dotnet run --project services/payment-service/src/PaymentService.Api    # :5501
dotnet run --project services/notification-service/src/NotificationService.Api  # :5301
```

Then the gateway:

```bash
dotnet run --project infrastructure/gateway/src/Platform.Gateway        # http://localhost:8080
```

(The gateway's `appsettings.Development.json` already points at those local ports.)

## 5. Run the Angular customer app

```bash
npm install                 # once, at the repo root (npm workspaces)
npm start --workspace=apps/angular-client/bus-ticketing-customer-web
# http://localhost:4200 — `ng serve` proxies /api -> http://localhost:8080 (the gateway)
```

Build a deployable bundle: `npm run build --workspace=apps/angular-client/bus-ticketing-customer-web`
(configurations: `production` default, `staging`, `development`).

## 6. Run the React admin app

```bash
npm install                 # once, at the repo root
npm run dev --workspace=apps/react-admin/bus-ticketing-admin
# http://localhost:5173 — Vite proxies /api -> http://localhost:8080 (the gateway)
```

Build: `npm run build --workspace=apps/react-admin/bus-ticketing-admin`
(env files: `.env.development`, `.env.staging`, `.env.production`).

There is **no seeded admin account**. To get one: register a normal user via
the customer app (or `POST http://localhost:8088/api/v1/auth/register`), then
promote it in Postgres — see
`apps/react-admin/bus-ticketing-admin/ai-handover.md` for the exact SQL. (A
proper bootstrap path is milestone M1.)

## 7. Where the gateway is exposed

- Docker: host **8088** → container **8080** (service name `api-gateway`).
- Local `dotnet run`: **http://localhost:8080**.
- The frontends' nginx (prod) and dev proxies point at it; **nothing else is
  public**. Internal service ports (5101/5201/… or `<service>:8080` in Docker)
  are never reachable by a browser.

## 8. How an API request flows

```
Browser  ──/api/v1/bookings/mine──►  frontend nginx / dev proxy
                                       │  (single upstream: the gateway)
                                       ▼
                              API Gateway (YARP)
                                 • X-Correlation-Id: validate or generate
                                 • strip client X-Tenant-Id; re-inject from JWT claim
                                 • edge rate-limit (IP / user / tenant)
                                 • route by path prefix  ──►  booking-service:8080
                                                                 │
                                                                 ▼  (service does its own
                                                              authN/authZ, business logic)
```

## 9. Verify `/health`

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8088/health         # gateway  -> 200
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8088/api/v1/auth/me # reaches auth-service (401 without a token = routing OK)
```

## 10. Verify correlation IDs

```bash
# generated when the client sends none, and returned on the response
curl -sD - -o /dev/null http://localhost:8088/ | grep -i x-correlation-id

# a valid client-supplied id is preserved through to the service
curl -sD - -o /dev/null -H 'X-Correlation-Id: demo-trace-0001' \
  http://localhost:8088/api/v1/auth/me | grep -i x-correlation-id

# a malformed id is replaced, not echoed
curl -sD - -o /dev/null -H 'X-Correlation-Id: bad id !!' \
  http://localhost:8088/ | grep -i x-correlation-id
```

Full detail: `docs/programmers-guide/correlation-id.md`.

## 11. Verify security headers

```bash
curl -sD - -o /dev/null http://localhost:8088/ | grep -iE \
  'x-content-type-options|x-frame-options|referrer-policy|content-security-policy|^server:'
# expect nosniff / DENY / no-referrer / "default-src 'none'"  and NO Server header
```

## 12. Verify RabbitMQ / event routing

```bash
# 1. Watch the notification-service logs
docker compose -f infrastructure/docker/docker-compose.yml logs -f notification-service

# 2. Trigger an event, e.g. register a user (auth publishes auth.user.registered)
curl -s -XPOST http://localhost:8088/api/v1/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"demo@example.com","password":"Passw0rd!23","firstName":"De","lastName":"Mo"}'

# 3. In RabbitMQ management (http://localhost:15672) the "auth.events" exchange
#    shows the message routed with key "auth.user.registered"; the
#    notification-service queue "notification-service.upstream-events" receives it.
```

Automated proof of the routing-key contract:
`dotnet test tests/platform/Platform.Messaging.IntegrationTests`
(spins a real RabbitMQ container). Detail: `docs/programmers-guide/messaging-contracts.md`.

## 13. Run unit tests

```bash
# every service
for s in auth booking bus route payment notification; do
  dotnet test services/$s-service/tests/${s^}Service.UnitTests 2>/dev/null \
    || dotnet test "services/$s-service/tests/"*UnitTests
done

# platform shared kernel + contracts (226 tests) and the gateway (21 tests)
dotnet test shared/Platform.Shared.sln
dotnet test infrastructure/gateway/Platform.Gateway.sln
```

## 14. Run integration tests

Require Docker (Testcontainers spins Postgres / RabbitMQ / Redis per test class).

```bash
# messaging contract, against a real broker
dotnet test tests/platform/Platform.Messaging.IntegrationTests

# per service
dotnet test services/auth-service/tests/AuthService.IntegrationTests
# ...etc. NOTE: 2 auth integration tests (Admin_ListPermissions,
# SecurityQuestions_ConfigureAndVerify) fail on a clean checkout too — a
# pre-existing issue, not caused by M0. Tracked in ai-handover.md.
```

## 15. Build production artifacts

```bash
# backend service images (via compose)
cd infrastructure/docker && docker compose build

# gateway image (build context = repo root — it references shared/*)
docker build -f infrastructure/gateway/Dockerfile -t platform-gateway .

# frontends
npm run build --workspace=apps/angular-client/bus-ticketing-customer-web
npm run build --workspace=apps/react-admin/bus-ticketing-admin
```

## 16. Common failures and fixes

| Symptom | Cause / fix |
|---------|-------------|
| Gateway won't start: *"Jwt:SigningKey is not configured"* | You're in `Production` with no key. Set `Jwt__SigningKey` (and `Jwt__Issuer`/`Jwt__Audience`). Development uses the dev key automatically. |
| `/api/v1/tickets/*` returns 502 | Expected — the Ticketing service doesn't exist yet (milestone M6). |
| Frontend can't reach the API in `ng serve` / `vite dev` | The gateway isn't running on `localhost:8080`. Start it (`dotnet run --project infrastructure/gateway/src/Platform.Gateway`) or run the Docker stack. |
| `docker compose up` fails building `auth-service`/`payment-service` with `useradd ... exit code 4` | Pre-existing Dockerfile bug (uid 1000 collision on the .NET 10 image). Run those two with `dotnet run` for now; fix is milestone M11. |
| Booking API returns 500 on every DB call | The `booking` schema isn't created. Run `dotnet ef database update --project services/booking-service/src/BookingService.Infrastructure --startup-project services/booking-service/src/BookingService.Api`. `20260903113152_InitialCreate` is checked in. |
| Every authenticated call → 401 across a service | `Jwt:SigningKey`/`Issuer`/`Audience` mismatch between that service and auth-service/the gateway. |
| RabbitMQ consumer logs *"unmapped routing key"* | A new upstream event was bound in `RabbitMq:UpstreamBindings` before its `RoutingKeyMap`/template entry was added. |
| Notification never sends despite the event arriving | No template is seeded (milestone M7). Create one via `POST /api/v1/templates`. |

## 17. What is intentionally deferred (do not "fix" as part of another task)

| Deferred | Milestone |
|----------|-----------|
| Redis-backed distributed rate limiting (gateway limiter is in-memory) | M9 |
| OTLP collector + Jaeger + Prometheus + Grafana (exporters point at a dead endpoint) | M8 |
| Correlation id carried *through the transactional outbox* to RabbitMQ | M2 / M9 |
| Consumer inbox de-duplication (redelivered events duplicate notifications) | M7 |
| booking-service `InitialCreate` migration + seat-hold concurrency fix | ✅ done 2026-09-03 (M2) |
| Payment confirm/refund/webhook safety; real bKash / Nagad / Bangla QR | M3 / M4 / M5 |
| Ticketing service (ticket number / QR / PDF / verification) | M6 |
| Per-service JWT signing keys; non-root + healthcheck on every Dockerfile; CI/CD | M11 |
| notification-service EF Core 9 → 10 | M7 |
| Frontend i18n (en/bn); token refresh; OTP UI | M1 / M10 |

---

## The 6 services and their DbContexts

| Service              | Project (Infrastructure)          | DbContext            | Default schema | Default dev port |
|-----------------------|-----------------------------------|-----------------------|-----------------|-------------------|
| auth-service           | `AuthService.Infrastructure`       | `AuthDbContext`         | `auth`            | 5101 |
| booking-service        | `BookingService.Infrastructure`    | `BookingDbContext`      | `booking`         | `20260903113152_InitialCreate` checked in — `dotnet ef database update` before first use |
| bus-service             | `BusService.Infrastructure`        | `BusDbContext`          | `bus`              | see its `launchSettings.json` |
| notification-service   | `NotificationService.Infrastructure`| `NotificationDbContext`| `notification`    | see its `launchSettings.json` |
| payment-service         | `PaymentService.Infrastructure`    | `PaymentDbContext`      | `payment`          | 5003 |
| route-service            | `RouteService.Infrastructure`      | `RouteDbContext`        | `route`            | see its `launchSettings.json` |

Every service follows the same project layout:
```
services/<name>/src/<Name>.Api             <- startup project (has appsettings.json, Program.cs)
services/<name>/src/<Name>.Infrastructure  <- migration project (has the DbContext + Migrations/ folder)
services/<name>/src/<Name>.Application
services/<name>/src/<Name>.Domain
```

## One-time setup

```bash
dotnet tool install --global dotnet-ef   # if you don't already have it
dotnet ef --version                      # confirm it resolves
```

Every service's connection string lives in that service's own
`appsettings.Development.json` under `ConnectionStrings:DefaultConnection`
(payment-service) or the service-specific equivalent — check that file
before running against a real database, and never commit a real
production connection string or signing key (the placeholders committed
here, e.g. `REPLACE_WITH_A_SECRET_AT_LEAST_32_CHARS_LONG_IN_PROD`, are
intentionally not usable in production).

## Creating a new migration — the exact command, per service

Run from the **repo root**. The pattern is identical for every service —
only the folder and project names change. Use a `dd-mm-yy-name` migration
name as your naming convention (EF Core migration names can't contain
spaces or most punctuation — hyphens are fine):

```bash
# auth-service
dotnet ef migrations add "dd-mm-yy-name" \
  --project services/auth-service/src/AuthService.Infrastructure \
  --startup-project services/auth-service/src/AuthService.Api \
  --output-dir Migrations

# booking-service
dotnet ef migrations add "dd-mm-yy-name" \
  --project services/booking-service/src/BookingService.Infrastructure \
  --startup-project services/booking-service/src/BookingService.Api \
  --output-dir Migrations

# bus-service
dotnet ef migrations add "dd-mm-yy-name" \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api \
  --output-dir Migrations

# notification-service
dotnet ef migrations add "dd-mm-yy-name" \
  --project services/notification-service/src/NotificationService.Infrastructure \
  --startup-project services/notification-service/src/NotificationService.Api \
  --output-dir Migrations

# payment-service
dotnet ef migrations add "dd-mm-yy-name" \
  --project services/payment-service/src/PaymentService.Infrastructure \
  --startup-project services/payment-service/src/PaymentService.Api \
  --output-dir Migrations

# route-service
dotnet ef migrations add "dd-mm-yy-name" \
  --project services/route-service/src/RouteService.Infrastructure \
  --startup-project services/route-service/src/RouteService.Api \
  --output-dir Migrations
```

Replace `"dd-mm-yy-name"` with today's date and a short description, e.g.
`"19-08-26-add-payment-method-limit"`. This keeps migration history
chronologically sortable in every service's `Migrations/` folder, which is
the convention already used by payment-service's existing migrations
(`20260810133345_InitialCreate`, `20260810195845_AddAgentPaymentMethod`).

**This is how every change reflects as a new migration and nothing gets
missed**: change the entity/configuration in the relevant `.Domain` /
`.Infrastructure` project first, then run the one command above for that
service. EF Core diffs your current model against the last-applied
migration and generates only the delta — you never hand-write migration
SQL for a normal schema change.

## Applying migrations — update every service's database from the root

Run each service's update after adding its migration, or run all 6 in one
pass (e.g. after pulling someone else's migrations):

```bash
dotnet ef database update \
  --project services/auth-service/src/AuthService.Infrastructure \
  --startup-project services/auth-service/src/AuthService.Api

dotnet ef database update \
  --project services/booking-service/src/BookingService.Infrastructure \
  --startup-project services/booking-service/src/BookingService.Api

dotnet ef database update \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api

dotnet ef database update \
  --project services/notification-service/src/NotificationService.Infrastructure \
  --startup-project services/notification-service/src/NotificationService.Api

dotnet ef database update \
  --project services/payment-service/src/PaymentService.Infrastructure \
  --startup-project services/payment-service/src/PaymentService.Api

dotnet ef database update \
  --project services/route-service/src/RouteService.Infrastructure \
  --startup-project services/route-service/src/RouteService.Api
```

Or, as one copy-pasteable loop from the repo root (bash):

```bash
for svc in auth-service:AuthService booking-service:BookingService \
           bus-service:BusService notification-service:NotificationService \
           payment-service:PaymentService route-service:RouteService; do
  dir="${svc%%:*}"; name="${svc##*:}"
  echo "== $dir =="
  dotnet ef database update \
    --project "services/$dir/src/$name.Infrastructure" \
    --startup-project "services/$dir/src/$name.Api"
done
```

Each service's Postgres schema (`auth`, `booking`, `bus`, `notification`,
`payment`, `route` — see the table above) is isolated by design (see
`docs/adr` for the "one schema per service, no cross-schema FKs" decision)
— running one service's `database update` never touches another
service's tables, so there is no required ordering between services
unless a specific migration says otherwise.

## Running a single service locally

```bash
cd services/<name>/src/<Name>.Api
dotnet run
```

Each `Api` project exposes, in the `Development` environment:
- `/scalar` — interactive API documentation (Scalar UI, replaces Swagger UI on this platform)
- `/openapi/v1.json` — the raw OpenAPI document
- `/health/live`, `/health/ready` — health probes

## Independent + wired-together operation

Every service can be built, migrated, and run **independently** — none of
the 6 `.sln` files reference another service's projects, and each has its
own database/schema and its own `appsettings.json`. What wires them
together at runtime is:

1. **auth-service issues the JWTs** every other service validates. All 5
   downstream services must have matching `Jwt:Issuer` / `Jwt:Audience` /
   `Jwt:SigningKey` values in their `appsettings*.json` — a mismatch here
   silently causes every authenticated call to that service to fail with
   401 even though the service itself is healthy (this exact bug existed
   in payment-service and was fixed in this pass — see
   `services/payment-service/docs/new-release-notes/release-notes.md`).
2. **The API gateway** (`infrastructure/gateway`) is **not yet implemented** —
   the directory is empty and there is no YARP/Ocelot project anywhere in the
   repo. Today each frontend's own nginx (prod) / dev proxy fans requests out
   to the individual service ports directly. Standing up a single YARP gateway
   is milestone **M0** in [`docs/PRODUCTION-MILESTONES.md`](docs/PRODUCTION-MILESTONES.md).
3. **RabbitMQ** carries domain events between services (each service's
   transactional outbox — see e.g.
   `services/payment-service/docs/programmers-guide/adr/0002-transactional-outbox.md`
   — publishes to it).

For local end-to-end testing across services, run each service's own
Postman collection under `services/<name>/docs/scripts/postman/` — start
with auth-service's `Login` request (or let a collection's pre-request
script auto-login for you, as payment-service's now does) to get a token,
then exercise the other services with it.

## Licensing, module permissions, and per-user/per-day/per-month rate limits

This platform's target design for subscription-based licensing,
module-level permission grants, and configurable per-user request/child-user
limits is documented as a proposal in
`docs/adr/0009-subscription-licensing-and-module-rate-limits.md` — read
that before implementing any part of it, since auth-service already has a
Modules/Roles/Permissions foundation (`AuthService.Application.Features.Admin`)
that this design extends rather than replaces.
