# Database Provider Factory

Every service picks its EF Core provider at startup from **one config key** —
no code change to switch. PostgreSQL is the platform's **primary** provider;
the others are wired for portability.

## Config

```jsonc
// appsettings.json  (or  Database__Provider  env var)
{
  "Database": { "Provider": "Postgres" },   // Postgres | SqlServer | MySql
  "ConnectionStrings": { "BookingDb": "Host=…;Port=5432;Database=…;Username=…;Password=…" }
}
```

| Value        | Provider package                          | Notes |
|--------------|-------------------------------------------|-------|
| `Postgres` (default) | `Npgsql.EntityFrameworkCore.PostgreSQL` | Primary. Uses native `xmin` for optimistic concurrency. |
| `SqlServer`  | `Microsoft.EntityFrameworkCore.SqlServer` | |
| `MySql`      | `Pomelo.EntityFrameworkCore.MySql` | Pinned to 9.0.0 until Pomelo ships EF 10 (NU1608 suppressed deliberately). |
| `Sqlite`     | not referenced by default | `SQLitePCLRaw.lib.e_sqlite3` currently carries GHSA-2m69-gcr7-jv3q with no fix. Add `Microsoft.EntityFrameworkCore.Sqlite` yourself and regenerate migrations for SQLite to use it. |
| `Oracle`     | not wired | Add `Oracle.EntityFrameworkCore`, a `case "oracle"` in `AddDatabase`, and Oracle-specific migrations. |
| `MongoDB`    | not applicable | The `Trip`/`Booking` aggregates are relational (transactional seat-hold invariants, `xmin` concurrency). A document store would need a different persistence model — out of scope. |

## Where it lives

`services/<svc>/src/<Svc>.Infrastructure/DependencyInjection.cs` → `AddDatabase(...)`.
The `switch` on `Database:Provider` calls the matching `options.Use*(...)` and
sets `MigrationsAssembly` + the per-service migrations history table
(`__ef_migrations_history` in the service's schema).

## Switching provider — checklist

1. Set `Database:Provider` + the matching connection string.
2. **Regenerate migrations for that provider** — migrations are provider-specific:
   ```bash
   # from repo root, see MIGRATIONS.md
   rm -r services/<svc>/src/<Svc>.Infrastructure/Migrations   # or keep per-provider folders
   dotnet ef migrations add InitialCreate \
     --project services/<svc>/src/<Svc>.Infrastructure \
     --startup-project services/<svc>/src/<Svc>.Api
   ```
3. `dotnet ef database update` against the new database.
4. Concurrency columns mapped to Postgres `xmin` (`Trip`, `Booking`, `TripSeat`)
   need a provider-appropriate token (`rowversion` on SQL Server, a `ulong`
   `[Timestamp]` on MySql). Adjust the `IEntityTypeConfiguration` for that provider.

An unknown provider value throws at startup with the list of supported values —
it never silently falls back.
