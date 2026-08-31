# Enterprise Transport Platform — Root Developer Guide

This is the one place a new developer needs to look to build, run, and
**create/apply a database migration for any of the 6 backend services**
without having to go hunting through each service's own docs first. Each
service also has its own more detailed `guide.md` — start here, then drill
into a service's own guide for anything specific to it.

## The 6 services and their DbContexts

| Service              | Project (Infrastructure)          | DbContext            | Default schema | Default dev port |
|-----------------------|-----------------------------------|-----------------------|-----------------|-------------------|
| auth-service           | `AuthService.Infrastructure`       | `AuthDbContext`         | `auth`            | 5101 |
| booking-service        | `BookingService.Infrastructure`    | `BookingDbContext`      | `booking`         | see its `launchSettings.json` — **no migration is checked in yet; run `dotnet ef migrations add "InitialCreate"` (command below) before first use** |
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
