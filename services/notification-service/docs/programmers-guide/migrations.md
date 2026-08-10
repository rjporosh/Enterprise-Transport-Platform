# Database Migrations

## Commands

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project src/NotificationService.Infrastructure \
  --startup-project src/NotificationService.Api

# Apply pending migrations
dotnet ef database update \
  --project src/NotificationService.Infrastructure \
  --startup-project src/NotificationService.Api

# List all migrations
dotnet ef migrations list \
  --project src/NotificationService.Infrastructure \
  --startup-project src/NotificationService.Api

# Remove the last migration (only if not applied to any database)
dotnet ef migrations remove \
  --project src/NotificationService.Infrastructure \
  --startup-project src/NotificationService.Api
```

## Provider-Specific Migrations

The current `InitialCreate` migration is Postgres-specific (uses `uuid`, `timestamp with time zone`, `xid`, `bytea`).

To generate a migration for a different provider:
1. Set `Database:Provider` in `appsettings.json` to the target provider
2. Remove the existing migrations (or use a new migration name)
3. Run the add command above

## Model Snapshot

`NotificationDbContextModelSnapshot.cs` is auto-generated. Do not edit manually — it is regenerated on every `dotnet ef migrations add`.

## Notes

- In Development, `Program.cs` auto-applies migrations on startup
- In Production, migrations must be applied via CI/CD or manual `dotnet ef database update`
- Never modify the schema without a migration
