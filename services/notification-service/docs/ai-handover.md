# Notification Service — AI Handover

## Service Identity

- **Service**: Notification Service
- **Purpose**: Reusable notification delivery platform (Email, SMS, Push)
- **Target framework**: .NET 10
- **Architecture**: Clean Architecture + CQRS (MediatR)
- **Database**: PostgreSQL (primary), SQL Server, MySQL supported via provider switch

## Key Files

| File | Purpose |
|---|---|
| `src/NotificationService.Domain/Entities/Notification.cs` | Aggregate root with full state machine |
| `src/NotificationService.Application/Features/Notifications/SendNotification/SendNotificationHandler.cs` | Core send handler — creates notification, resolves template, enqueues outbox; inbox dedup |
| `src/NotificationService.Infrastructure/Scheduling/Jobs/NotificationDispatchJob.cs` | Claim-then-send dispatch — MarkSending+save before channel, MarkSent/Failed+save after |
| `src/NotificationService.Infrastructure/Messaging/NotificationEventConsumer.cs` | RabbitMQ consumer; extracts EventId for unique SourceReference |
| `src/NotificationService.Infrastructure/Persistence/CoreTemplateSeeder.cs` | Seeds 18 core templates (9 events × en + bn) on startup |
| `src/NotificationService.Api/Program.cs` | Composition root |
| `src/NotificationService.Api/Middleware/ExceptionHandlingMiddleware.cs` | Centralized error handler |

## Build & Test

```bash
# From solution root
dotnet build services/notification-service/NotificationService.sln
dotnet test services/notification-service/tests/NotificationService.UnitTests
```

## Current State (M7 — 2026-09-03)

- **Build**: 0 errors, 0 warnings
- **Unit tests**: 29/29 pass
- **Integration tests**: 5 pass (requires Docker/Testcontainers)
- **EF Core**: 10.0.0 (upgraded from 9.0.0)
- **Quartz**: 3.14.0
- All core templates seeded (en + bn)
- SMS providers: Twilio, GenericHttp, Bd (Bangladesh aggregators)
- Dispatch job uses claim-then-send (crash-safe)
- Inbox dedup via unique SourceReference per event id

## Migrations

| Migration | Description |
|---|---|
| `20260807133500_InitialCreate` | Core tables: notifications, templates, preferences, logs, outbox |
| `20260903124556_FixTemplateRowVersionConcurrency` | Template row-version concurrency fix |
| `20260903xxxxxx_AddUniqueSourceReferenceIndex` | Unique filtered index on SourceReference for inbox dedup |

```bash
# Apply from solution root
dotnet ef database update \
  --project services/notification-service/src/NotificationService.Infrastructure \
  --startup-project services/notification-service/src/NotificationService.Api
```

## Known Gaps (remaining)

1. Auth Service `GET /api/v1/users/{id}/contact` endpoint not implemented — recipient resolution from Booking/Payment events falls back to inline contact fields
2. In-memory idempotency cache (single-instance only — Redis backing needed for M9)
3. Oracle and MongoDB providers not wired
4. No delivery receipt webhook receivers
5. `Sms:Provider=Bd` requires live aggregator credentials to deliver

## Environment Variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__NotificationDb` | Database connection string |
| `Database__Provider` | Postgres (default), SqlServer, MySql |
| `RabbitMq__HostName` | RabbitMQ host |
| `Smtp__Host` | SMTP server (MailHog on localhost:1025 for dev) |
| `Sms__Provider` | Twilio \| GenericHttp \| Bd |
| `Sms__Bd__Endpoint` | Bangladesh aggregator send URL |
| `Sms__Bd__ApiToken` | Aggregator API token |
| `Push__FirebaseProjectId` | FCM project ID |

## Next Milestone: M8 — Observability Backend

Add OTel Collector + Jaeger + Prometheus + Grafana to docker-compose, fix prometheus.yml scrape targets, add Seq/Loki log sink, propagate `traceparent` in RabbitMQ message headers so booking→payment→notification→ticket flows appear as one connected trace in Jaeger.
