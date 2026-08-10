# Migrations

## Generating a migration

Route Service uses EF Core with a provider-switchable configuration. Migrations are
provider-specific and must be generated against the target provider.

```bash
# Postgres
dotnet ef migrations add InitialCreate \
  --project src/RouteService.Infrastructure \
  --startup-project src/RouteService.Api \
  --output-dir Migrations/Postgres

# SqlServer
dotnet ef migrations add InitialCreate \
  --project src/RouteService.Infrastructure \
  --startup-project src/RouteService.Api \
  --output-dir Migrations/SqlServer \
  --context RouteDbContext

# MySQL
dotnet ef migrations add InitialCreate \
  --project src/RouteService.Infrastructure \
  --startup-project src/RouteService.Api \
  --output-dir Migrations/MySql \
  --context RouteDbContext
```

## Applying migrations

In Development, migrations are applied automatically on startup. In Production,
use:

```bash
dotnet ef database update \
  --project src/RouteService.Infrastructure \
  --startup-project src/RouteService.Api
```

## Notes
- The `Database:Provider` appsetting controls which provider is used.
- Switching providers in an existing deployment requires regenerating migrations.
- Oracle is documented-but-not-wired (same rationale as other services).
