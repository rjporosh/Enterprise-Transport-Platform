# Route Service - Programmer Guide

## Project Structure

```
services/route-service/
├── RouteService.sln
├── src/
│   ├── RouteService.Domain/         # Entities, enums, events, exceptions, interfaces
│   ├── RouteService.Application/   # CQRS handlers, validators, DTOs, DI registration
│   ├── RouteService.Infrastructure/ # EF Core, Redis, RabbitMQ, Polly, OpenTelemetry
│   └── RouteService.Api/            # Minimal API endpoints, gRPC, middleware, auth
├── tests/
│   ├── RouteService.UnitTests/
│   └── RouteService.IntegrationTests/
├── docs/
│   └── programmers-guide/
│       ├── developer-guide.md
│       ├── c4-diagrams.md
│       ├── er-diagram.md
│       ├── postman-collection.json
│       ├── release-notes.md
│       ├── db-schema.md
│       └── operations.md
└── README.md
```

## Creating a New CRUD Feature

Follow the existing feature-folder pattern under `src/RouteService.Application/Features/`.

### 1. Domain (if new entity)

Create `src/RouteService.Domain/Entities/{Entity}.cs`:
- Inherit from `AggregateRoot`
- Implement `IAuditable` if soft-delete/audit is needed
- Raise domain events via `Raise(new ...DomainEvent(...))`
- Enforce invariants in the entity methods, not in handlers

### 2. Application

Create `src/RouteService.Application/Features/{Entity}/{Action}/{Action}Command.cs` (or Query):
```csharp
public sealed record CreateRouteCommand(...) : IRequest<Result<RouteDto>>;
```

Create `{Action}Handler.cs`:
- Inject `IRouteDbContext`, `IDateTimeProvider`, `IAuditLogger`, `ICurrentUser`, `ILogger<T>`
- Load entity, apply domain method, `SaveChangesAsync`, audit log

Create `{Action}Validator.cs` (FluentValidation).

### 3. Infrastructure

If new entity, add `src/RouteService.Infrastructure/Persistence/Configurations/{Entity}Configuration.cs`:
- Map table, keys, indexes, conversions
- Call `builder.Ignore(x => x.DomainEvents)` on aggregates
- **Do NOT add `HasQueryFilter` for soft-delete** — filter explicitly in handlers

### 4. API Endpoint

Add to `src/RouteService.Api/Endpoints/{Entity}Endpoints.cs`:
```csharp
entityGroup.MapPost("/", CreateAsync)
    .WithName("Create{Entity}")
    .Produces<{Entity}Dto>(StatusCodes.Status200OK)
    .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));
```

### 5. Database Migration

```bash
cd services/route-service
dotnet ef migrations add Add{Entity} --project src/RouteService.Infrastructure --startup-project src/RouteService.Api
dotnet ef database update --project src/RouteService.Infrastructure --startup-project src/RouteService.Api
```

### 6. Tests

Add unit tests under `tests/RouteService.UnitTests/{Entity}/`:
- Constructor wiring
- Happy path
- Not found / validation / concurrency failures

## Adding a Background Job / CronJob

Route Service uses .NET `IHostedService` for background work (same pattern as
Bus Service).

### 1. Create the worker

```csharp
// src/RouteService.Infrastructure/Messaging/OutboxProcessor.cs
public sealed class OutboxProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // process outbox messages
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

### 2. Register in DI

In `src/RouteService.Infrastructure/DependencyInjection.cs`:
```csharp
services.AddHostedService<OutboxProcessor>();
```

### 3. Run on schedule (Quartz.NET — future)

If you need cron-like scheduling (e.g. "activate due schedules every minute"),
add Quartz:

```bash
dotnet add package Quartz --version 3.13.1
dotnet add package Quartz.Extensions.Hosting --version 3.13.1
```

Then define a job:
```csharp
public class ActivateDueSchedulesJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // load schedules where EffectiveFrom <= now && Status == Planned
        // call schedule.Activate(clock.UtcNow)
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService();
```

## Switching Database Provider

Change `Database:Provider` in `appsettings.json` (or environment variable):

| Provider | Value | Connection String Format |
|----------|-------|--------------------------|
| PostgreSQL | `Postgres` (default) | `Host=localhost;Port=5432;Database=route_service;Username=postgres;Password=postgres` |
| SQL Server | `SqlServer` | `Server=localhost;Database=route_service;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true` |
| MySQL | `MySql` | `Server=localhost;Port=3306;Database=route_service;User=root;Password=root` |

After changing provider, generate a new migration:
```bash
dotnet ef migrations add ProviderSwitch --project src/RouteService.Infrastructure --startup-project src/RouteService.Api
dotnet ef database update --project src/RouteService.Infrastructure --startup-project src/RouteService.Api
```

## Changing Port / Hostname

Edit `Properties/launchSettings.json` or set environment variables:
```bash
export ASPNETCORE_URLS=http://0.0.0.0:5000
```

## Observability Quick Reference

| Tool | Endpoint / Path | Purpose |
|------|----------------|---------|
| Scalar Docs | `/scalar` (Dev only) | Interactive API explorer |
| OpenAPI JSON | `/openapi/v1.json` | Raw OpenAPI document |
| Health | `/health` | Liveness/readiness |
| Prometheus | `/metrics` | Metrics scrape |
| gRPC | `:5001` (default) | gRPC service |
| Runtime Logs | `logs/runtime-error-*.txt` | Startup/crash logs |
| Query Logs | `logs/query-log-*.txt` | SQL + duration (enable via `Logging:EnableQueryLogging=true`) |

## Localization

Route Service supports Bangla (bn) and English (en). Add translations in:

```
src/RouteService.Infrastructure/Localization/
├── Resources/
│   ├── Routes.en.resx
│   ├── Routes.bn.resx
│   ├── Stops.en.resx
│   └── Stops.bn.resx
```

Request culture via `Accept-Language: bn` header. The
`LocalizationMiddleware` sets `CultureInfo.CurrentCulture` before the
endpoint runs.

## Load / Stress Testing

Place k6, JMeter, or NBomber scripts under `tests/load/`. Example k6:

```javascript
import http from 'k6/http';
import { check } from 'k6';

export const options = { stages: [{ duration: '30s', target: 50 }] };

export default function () {
  const res = http.get('http://localhost:5000/api/v1/routes?page=1&pageSize=20');
  check(res, { 'status is 200': (r) => r.status === 200 });
}
```

Run:
```bash
k6 run tests/load/route-load-test.js
```

## Debugging

1. **Startup crash:** Check `logs/runtime-error-<date>.txt` in the API project directory.
2. **Slow query:** Enable `Logging:EnableQueryLogging=true` and check `logs/query-log-<date>.txt`.
3. **EF Core model errors:** The runtime error writer auto-detects model validation failures.
4. **gRPC errors:** Use `grpcurl` or a gRPC client (e.g. `kreya`) against port 5001.
