# Database migrations — command reference

All commands below are written to run from the **repository root** — no
`cd` into a service folder required — using EF Core CLI's `--project` /
`--startup-project` flags. Copy-paste the block for whichever service you
need.

## Prerequisite (one-time)

```bash
dotnet tool install --global dotnet-ef
```

## Bus Service

Already has a committed `InitialCreate` migration
(`services/bus-service/src/BusService.Infrastructure/Migrations/`) — for a
fresh Postgres database, skip straight to **Apply** below. Only regenerate
if you've changed an entity or `IEntityTypeConfiguration<T>`.

```bash
# Apply the committed migration(s) to your database
dotnet ef database update \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api \
  --context BusDbContext

# Add a new migration after changing the entity model
dotnet ef migrations add <DescriptiveName> \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api \
  --context BusDbContext \
  --output-dir Migrations

# Remove the most recently added (not-yet-applied) migration
dotnet ef migrations remove \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api \
  --context BusDbContext
```

## Auth Service

Already has a committed `InitialCreate` migration
(`services/auth-service/src/AuthService.Infrastructure/Migrations/`).

```bash
dotnet ef database update \
  --project services/auth-service/src/AuthService.Infrastructure \
  --startup-project services/auth-service/src/AuthService.Api \
  --context AuthDbContext

dotnet ef migrations add <DescriptiveName> \
  --project services/auth-service/src/AuthService.Infrastructure \
  --startup-project services/auth-service/src/AuthService.Api \
  --context AuthDbContext \
  --output-dir Migrations
```

## Booking Service

**No migration committed yet** — generate one before first run:

```bash
dotnet ef migrations add InitialCreate \
  --project services/booking-service/src/BookingService.Infrastructure \
  --startup-project services/booking-service/src/BookingService.Api \
  --context BookingDbContext \
  --output-dir Migrations

dotnet ef database update \
  --project services/booking-service/src/BookingService.Infrastructure \
  --startup-project services/booking-service/src/BookingService.Api \
  --context BookingDbContext
```

## Switching database provider (Auth Service, Bus Service)

Both services support `Database:Provider` = `Postgres` | `SqlServer` |
`MySql` (see each service's `docs/architecture/*-architecture.md`,
"Database portability"). **Migrations are provider-specific** — the
committed `InitialCreate` migrations target Postgres. Switching provider
means regenerating migrations for the new provider, not just changing the
config value:

```bash
# Example: generate a SQL Server migration for Bus Service
# 1. Set Database:Provider to SqlServer in appsettings.json (or via
#    ASPNETCORE_Database__Provider env var) first, so dotnet ef picks the
#    right provider when scaffolding.
# 2. Delete or move aside the existing Postgres-targeted Migrations/ folder
#    contents (a project can only have one provider's migrations active
#    at a time in the default single Migrations/ folder — see the EF Core
#    docs on multiple providers if you need to support switching between
#    already-deployed environments on different providers simultaneously).
dotnet ef migrations add InitialCreate \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api \
  --context BusDbContext \
  --output-dir Migrations

dotnet ef database update \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api \
  --context BusDbContext
```

## Useful extras

```bash
# See what SQL a migration would actually run, without applying it
dotnet ef migrations script \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api \
  --context BusDbContext

# Roll back to a specific earlier migration (use the migration's name, or
# "0" to unapply everything)
dotnet ef database update <PreviousMigrationName> \
  --project services/bus-service/src/BusService.Infrastructure \
  --startup-project services/bus-service/src/BusService.Api \
  --context BusDbContext
```

## Auto-apply on startup (Development only)

Every service's `Program.cs` calls `Database.MigrateAsync()` automatically
when `ASPNETCORE_ENVIRONMENT=Development` — so for local dev, `dotnet run`
alone applies any pending migration; you don't need to run
`dotnet ef database update` by hand every time, only after adding a *new*
migration for the first time (so it exists in the `Migrations/` folder to
be picked up) or when working against a database the service hasn't
started against yet. This auto-apply is intentionally **not** enabled
outside Development — schema changes in staging/production should go
through a reviewable CI/CD step, not happen implicitly on an app restart.
