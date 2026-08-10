# Notification Service — AI Handover

## Service Identity

- **Service**: Notification Service
- **Purpose**: Reusable notification delivery platform (Email, SMS, Push, In-App)
- **Target framework**: .NET 10
- **Architecture**: Clean Architecture + CQRS (MediatR)
- **Database**: PostgreSQL (primary), SQL Server, MySQL supported via provider switch

## Key Files

| File | Purpose |
|---|---|
| `src/NotificationService.Domain/Entities/Notification.cs` | Aggregate root with full state machine |
| `src/NotificationService.Application/Features/Notifications/SendNotification/SendNotificationHandler.cs` | Core send handler — creates notification, resolves template, enqueues outbox |
| `src/NotificationService.Infrastructure/Scheduling/Jobs/NotificationDispatchJob.cs` | Single dispatch point for all outbound sends |
| `src/NotificationService.Api/Program.cs` | Composition root |
| `src/NotificationService.Api/Middleware/ExceptionHandlingMiddleware.cs` | Centralized error handler |

## Build & Test

```bash
dotnet build
dotnet test
```

## Current State

- Build passes (0 errors)
- 27 unit tests pass
- 5 integration tests pass (requires Docker/Testcontainers)
- k6 load test exists
- NBomber and JMeter load tests added

## Known Gaps

1. Auth Service contact endpoint not implemented (blocks Booking/Payment event recipient resolution)
2. In-memory idempotency cache (not Redis-backed)
3. Oracle and MongoDB providers not wired
4. No delivery receipt webhook receivers

## Environment Variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__NotificationDb` | Database connection string |
| `Database__Provider` | Postgres, SqlServer, MySql |
| `RabbitMq__HostName` | RabbitMQ host |
| `Smtp__Host` | SMTP server |
| `Sms__Provider` | Twilio or GenericHttp |
| `Push__FirebaseProjectId` | FCM project ID |
