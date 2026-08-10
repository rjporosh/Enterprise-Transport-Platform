using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Release;

namespace NotificationService.Application.Features.Release;

public sealed class GetReleaseInfoHandler : IRequestHandler<GetReleaseInfoQuery, Result<ReleaseInfoDto>>
{
    public Task<Result<ReleaseInfoDto>> Handle(GetReleaseInfoQuery request, CancellationToken cancellationToken)
    {
        var info = new ReleaseInfoDto(
            ServiceName: "Notification Service",
            Version: "1.0.0",
            ReleaseDate: "2026-08-10",
            ReleaseIdentifier: "notification-service-v1.0.0",
            NewFeatures: new List<string>
            {
                "Email notification delivery via SMTP (MailKit)",
                "SMS notification delivery via Twilio and Generic HTTP providers",
                "Push notification delivery via Firebase Cloud Messaging HTTP v1",
                "Notification templates with Scriban server-side rendering",
                "Template locale fallback (requested locale -> English)",
                "Recipient preferences with channel opt-out support",
                "Quartz.NET scheduled notification dispatch (every 10s)",
                "Stuck notification recovery job (every 5 min)",
                "Automatic retry with exponential backoff (capped at 60 min)",
                "Dead-letter queue for exhausted retries",
                "Outbox pattern for reliable event publishing",
                "RabbitMQ upstream event consumer (Auth/Booking/Payment events)",
                "REST API with pagination, filtering, and search",
                "gRPC SendNotification and GetNotificationStatus endpoints",
                "Result Pattern with multi-error validation responses",
                "Centralized exception handling (RFC 7807 ProblemDetails)",
                "Soft delete for notifications and templates",
                "Optimistic concurrency (xmin rowversion for notifications, bytea rowversion for templates)",
                "Serilog structured logging with daily rolling files",
                "OpenTelemetry distributed tracing and Prometheus metrics",
                "Health checks for PostgreSQL and RabbitMQ",
                "Multi-provider database abstraction (PostgreSQL, SQL Server, MySQL)",
                "Provider abstraction for email, SMS, and push channels",
                "Idempotency-Key middleware for write operations",
                "Rate limiting (60 writes/min per IP)",
                "CorrelationId propagation across all requests",
                "Localization support (English, Bangla)"
            },
            ChangedFeatures: new List<string>(),
            BugFixes: new List<string>(),
            ApiChanges: new List<string>
            {
                "POST /api/v1/notifications — send or schedule a notification",
                "GET /api/v1/notifications/{id} — retrieve notification with delivery logs",
                "GET /api/v1/notifications — paged, filterable, searchable listing",
                "POST /api/v1/notifications/{id}/cancel — cancel pending/scheduled/retrying notification",
                "POST /api/v1/notifications/{id}/retry — manual retry for dead-lettered notifications",
                "POST /api/v1/notifications/{id}/delete — soft-delete notification",
                "POST /api/v1/templates — create template",
                "PUT /api/v1/templates/{id} — update template (increments version)",
                "GET /api/v1/templates/{id} — retrieve template",
                "GET /api/v1/templates — paged, filterable, searchable template listing",
                "DELETE /api/v1/templates/{id} — soft-delete template",
                "GET /api/v1/recipients/{recipientId}/preferences — get recipient preferences",
                "PUT /api/v1/recipients/{recipientId}/preferences — update recipient preferences",
                "gRPC: SendNotification — internal synchronous send",
                "gRPC: GetNotificationStatus — retrieve notification status",
                "GET /api/v1/release — SQA release information endpoint (new)"
            },
            DatabaseChanges: new List<string>
            {
                "Initial migration: 20260807133500_InitialCreate",
                "Schema: notification.notifications, notification.notification_templates, notification.recipient_preferences, notification.notification_logs, notification.outbox_messages",
                "Optimistic concurrency: xmin rowversion on notifications, bytea rowversion on templates",
                "Soft delete: IsDeleted flag with global query filters on notifications and templates",
                "Indexes: notifications (Recipient, Status, CreatedAtUtc, SourceReference, Status_NextRetryAtUtc, Status_ScheduledForUtc), templates (Key_Channel_Locale unique), logs (NotificationId), outbox (ProcessedOnUtc_RetryCount), preferences (RecipientId unique)"
            },
            ConfigurationChanges: new List<string>
            {
                "Database:Provider — selects EF Core provider (Postgres, SqlServer, MySql)",
                "RabbitMq:HostName, Port, UserName, Password, UpstreamBindings",
                "Smtp:Host, Port, UserName, Password, UseStartTls, FromAddress, FromDisplayName",
                "Sms:Provider (Twilio | GenericHttp), FromNumber, provider credentials",
                "Push:FirebaseProjectId, ServiceAccountJsonPath",
                "UserDirectory:BaseUrl — for recipient contact resolution (requires Auth Service endpoint)",
                "Retry:MaxAttempts, Retry:BaseDelayMilliseconds — Polly retry config",
                "Jwt:Issuer, Audience, SigningKey — for operator-facing endpoints",
                "OpenTelemetry:OtlpEndpoint"
            },
            TestingNotes: "Unit tests: 27 passing (domain state machine, handlers, template operations). Integration tests: 5 Testcontainers-based tests (requires Docker). Load tests: k6 send-throughput test (25 VUs, p95 < 300ms). NBomber and JMeter scenarios provided in tests/load-test/.",
            BreakingChanges: new List<string>(),
            KnownLimitations: new List<string>
            {
                "Auth Service GET /api/v1/users/{id}/contact endpoint does not exist yet — Booking/Payment event recipient resolution falls back to inline contact fields or acks-and-drops",
                "Idempotency-Key cache is in-memory (ConcurrentDictionary) — breaks across replicas; Redis backing needed for production multi-instance",
                "Oracle and MongoDB database providers are not wired — requires separate architecture decision",
                "FCM push and Twilio/SMS integrations require real credentials to deliver",
                "Search uses ToLower().Contains() instead of EF.Functions.ILike for cross-provider compatibility — no case-insensitive index on PostgreSQL"
            });

        return Task.FromResult(Result<ReleaseInfoDto>.Success(info));
    }
}
