# PaymentService — Build, Run & Migration Guide

## Prerequisites

- .NET 10 SDK
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`
- A reachable PostgreSQL instance (default provider) — or SQL Server / SQLite / MySQL if you switch `ConnectionStrings:Provider`

## Restore, Build, Test

```bash
# From the repo root (payment-service/)
dotnet restore PaymentService.sln

# Build with warnings/errors treated as visible (no -v q, so you see everything)
dotnet build PaymentService.sln -c Release

# Run the full test suite
dotnet test PaymentService.sln -c Release
```

Expected result: **0 Warning(s), 0 Error(s)** in the build summary. If you see any, capture the full `dotnet build` output and check `release-notes.md` / `ai-hanover.md` in this repo for known open items before assuming it's a new regression.

## Run the API

```bash
dotnet run --project src/PaymentService.Api
```

Swagger/Scalar UI is available at `/scalar` in Development; health checks at `/health/live` and `/health/ready`.

## Database Migrations (EF Core)

The `PaymentService.Infrastructure` project holds the `PaymentDbContext` and migrations; `PaymentService.Api` is the startup project (for its configuration), but a design-time factory (`PaymentDbContextDesignTimeFactory`) is also provided so migrations can be generated without needing the API's DI container to resolve.

**Add a new migration:**
```bash
dotnet ef migrations add <MigrationName> \
  --project src/PaymentService.Infrastructure \
  --startup-project src/PaymentService.Api \
  --output-dir Migrations
```

**Apply migrations to the database (update DB):**
```bash
dotnet ef database update \
  --project src/PaymentService.Infrastructure \
  --startup-project src/PaymentService.Api
```

**Roll back to a specific migration:**
```bash
dotnet ef database update <PreviousMigrationName> \
  --project src/PaymentService.Infrastructure \
  --startup-project src/PaymentService.Api
```

**Generate a SQL script instead of applying directly (useful for prod):**
```bash
dotnet ef migrations script \
  --project src/PaymentService.Infrastructure \
  --startup-project src/PaymentService.Api \
  --output migration.sql \
  --idempotent
```

### Connection string / provider

Provider is selected at runtime via `ConnectionStrings:Provider` (`postgresql` | `sqlserver` | `sqlite` | `mysql`) — see `src/PaymentService.Infrastructure/DependencyInjection.cs`. For migration generation specifically, `PaymentDbContextDesignTimeFactory` hardcodes a local PostgreSQL connection string (`Host=localhost;Port=5432;Database=payment_db;Username=postgres;Password=postgres`) — edit that file if your local Postgres differs, or set the `ConnectionStrings__Default` / provider env vars the design-time factory reads if you've wired that in.

## More detail

See `docs/programmers-guide/developer-guide.md` for adding new features/providers, and `docs/programmers-guide/release-notes.md` for what's shipped.

## IMPORTANT — verification status of this guide

This guide's commands are the standard, correct commands for this project shape (verified by reading every `.csproj`, `Program.cs`, `PaymentDbContext`, and `PaymentDbContextDesignTimeFactory` in the solution). However, **they have not been executed in this environment** — see `ai-hanover.md` for why and what to run first.
