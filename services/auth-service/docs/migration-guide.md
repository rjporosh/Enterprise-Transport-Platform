# Migration Commands

Run these commands from the repository root.

## Add a new migration

```bash
cd services/auth-service
dotnet ef migrations add <MigrationName> \
  --project src/AuthService.Infrastructure/AuthService.Infrastructure.csproj \
  --output-dir src/AuthService.Infrastructure/Migrations
```

## Update database (apply pending migrations)

```bash
cd services/auth-service
dotnet ef database update \
  --project src/AuthService.Infrastructure/AuthService.Infrastructure.csproj
```

## List all migrations

```bash
cd services/auth-service
dotnet ef migrations list \
  --project src/AuthService.Infrastructure/AuthService.Infrastructure.csproj
```

## Remove last migration (only if not applied to any database)

```bash
cd services/auth-service
dotnet ef migrations remove \
  --project src/AuthService.Infrastructure/AuthService.Infrastructure.csproj
```

## Current Migrations

1. `20260802071041_InitialCreate` — baseline schema
2. `20260810134211_AddSecurityAdminFeatures` — permissions, modules, OTP, security questions, password history, reset tokens, sessions, claims, role/module permissions

## Database Provider Switching

Edit `appsettings.json` or environment variables:

```json
{
  "Database": {
    "Provider": "Postgres"  // "SqlServer" | "MySql"
  },
  "ConnectionStrings": {
    "AuthDb": "Host=localhost;Database=auth_service;Username=postgres;Password=postgres"
  }
}
```

Switching providers requires regenerating migrations because SQL dialects differ.
