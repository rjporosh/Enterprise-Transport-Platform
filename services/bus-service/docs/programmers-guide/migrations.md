# Programmer's Guide — Migrations

## Creating a Migration

```bash
cd services/bus-service/src/BusService.Infrastructure
dotnet ef migrations add AddNewField --startup-project ../BusService.Api
```

## Applying Migrations

```bash
dotnet ef database update --startup-project ../BusService.Api
```

## Listing Migrations

```bash
dotnet ef migrations list --startup-project ../BusService.Api
```

## Removing the Last Migration

Only safe if the migration has not been applied to any database:

```bash
dotnet ef migrations remove --startup-project ../BusService.Api
```

## Provider Notes

Because Bus Service supports Postgres, SQL Server, and MySQL, migrations are provider-specific. If you switch providers, delete existing migrations and regenerate them for the new provider.

```bash
# Remove all migrations
rm -rf Migrations/*

# Regenerate for Postgres
dotnet ef migrations add InitialCreate --startup-project ../BusService.Api
```

## Auto-Migration in Development

In Development, `Program.cs` automatically applies pending migrations on startup:

```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BusDbContext>();
    await db.Database.MigrateAsync();
}
```

Disable this in production; use CI/CD or `dotnet ef database update` instead.
