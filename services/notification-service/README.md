# Notification Service

Email/SMS/Push delivery, templates, retry/outbox, and event-driven
notifications for the Enterprise Transport Platform. Built with .NET 10,
Clean Architecture, and CQRS (MediatR) — see
[`docs/architecture/notification-service-architecture.md`](../../docs/architecture/notification-service-architecture.md)
for the full design rationale.

> **Build status note**: this was built in a sandbox with no .NET SDK and no
> network access — every file was hand-reviewed against this repo's own
> established conventions (`AuthService`/`BookingService`, read directly, not
> from memory), but **not compiled, not run, and not tested**. Run a clean
> `dotnet build` and `dotnet test` before deploying. See "Known limitations"
> below for the specific things most likely to need a fix once a compiler is
> in the loop.

## What's here

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `NotificationService.Domain` | `Notification` (send/retry/cancel state machine), `NotificationTemplate`, `RecipientPreference`, `NotificationLog` — zero framework deps |
| Application | `NotificationService.Application` | CQRS: Send/Get/Cancel/Retry/Delete notifications; Create/Update/Get/Delete templates; Get/Update recipient preferences |
| Infrastructure | `NotificationService.Infrastructure` | EF Core (Postgres/SqlServer/MySql switch), outbox + RabbitMQ (publish and an upstream-event consumer), SMTP (MailKit) / Twilio+GenericHttp SMS / FCM HTTP v1 push, Scriban templates, Polly retry, Quartz jobs, resx localization (en, bn) |
| Api | `NotificationService.Api` | REST + gRPC endpoints, JWT bearer auth, rate limiting, native OpenAPI+Scalar, health checks, OpenTelemetry+Prometheus |
| Tests | `NotificationService.UnitTests`, `NotificationService.IntegrationTests` | Handler/state-machine unit tests (EF InMemory), Testcontainers-based API tests |
| Load tests | `tests/load/k6` | Send-throughput load test — see `tests/load/README.md` |

## Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/notifications` | — | Send or schedule a notification |
| GET | `/api/v1/notifications/{id}` | — | Get one notification + its delivery-attempt log |
| GET | `/api/v1/notifications` | — | Paged/filtered/searchable notification history |
| POST | `/api/v1/notifications/{id}/cancel` | — | Cancel before it sends |
| POST | `/api/v1/notifications/{id}/retry` | Bearer | Give a DeadLettered notification a fresh retry budget |
| POST | `/api/v1/notifications/{id}/delete` | Bearer | Soft-delete |
| POST/PUT/GET/DELETE | `/api/v1/templates`, `/api/v1/templates/{id}` | Bearer | Template CRUD + paged listing |
| GET/PUT | `/api/v1/recipients/{recipientId}/preferences` | — | Channel opt-in/out + locale |
| gRPC | `notification.NotificationGrpcService/SendNotification`, `/GetNotificationStatus` | — | Internal service-to-service (see architecture doc §8) |
| GET | `/health` | — | Liveness/readiness (Postgres, RabbitMQ) |
| GET | `/metrics` | — | Prometheus scrape endpoint |
| GET | `/scalar` | — (Development only) | Interactive API docs |

## Running locally

**No migration is committed yet** (see "Known limitations" — this sandbox
has no `dotnet-ef` tool available). Generate one before first run:

```bash
dotnet tool install --global dotnet-ef   # one-time, if you don't have it
cd services/notification-service/src/NotificationService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../NotificationService.Api --context NotificationDbContext
```

```bash
# 1. Start dependencies (from repo root)
docker compose -f infrastructure/docker/docker-compose.yml up -d postgres rabbitmq mailhog

# 2. Run the API — applies the migration automatically in Development (see Program.cs)
cd services/notification-service/src/NotificationService.Api
dotnet run
# → http://localhost:5301/scalar
```

## Running tests

```bash
cd services/notification-service
dotnet test tests/NotificationService.UnitTests
dotnet test tests/NotificationService.IntegrationTests   # needs Docker (Testcontainers)
```

See [`tests/load/README.md`](tests/load/README.md) for the k6 load test.

## Configuration

All config lives in `src/NotificationService.Api/appsettings.json`,
overridable via environment variables (`Smtp__Host`, `Sms__Provider`,
`Push__FirebaseProjectId`, `ConnectionStrings__NotificationDb`,
`Database__Provider`, etc.) or `dotnet user-secrets` locally.

| Key | Default | Notes |
|---|---|---|
| `Database:Provider` | `Postgres` | `Postgres` \| `SqlServer` \| `MySql` — see architecture doc §4 |
| `Smtp:*` | local MailHog (`localhost:1025`) | Point at a real relay (SendGrid/SES/Postmark SMTP, etc.) in any non-dev environment |
| `Sms:Provider` | `GenericHttp` | `Twilio` \| `GenericHttp` — see architecture doc §8-adjacent `SmsSenderFactory` |
| `Push:FirebaseProjectId`, `Push:ServiceAccountJsonPath` | empty | Required for push to work at all — see `FcmPushSender` |
| `RabbitMq:UpstreamBindings` | Auth/Booking/Payment routing keys | What upstream events this service reacts to — see architecture doc §5 |
| `UserDirectory:BaseUrl` | empty | Not yet backed by a real Auth Service endpoint — see architecture doc §5 |
| `Retry:MaxAttempts`, `Retry:BaseDelayMilliseconds` | `3`, `500` | In-process Polly retry per channel-provider call |

## Known limitations

Being transparent about what this delivery does **not** include, rather than
silently shipping something that looks more finished than it is:

1. **Not compiled or tested.** No .NET SDK/network access in the sandbox
   this was built in. Run `dotnet build`/`dotnet test` before trusting any
   of it — see the note at the top of this file.
2. **No EF Core migration committed.** `dotnet-ef` requires the SDK, which
   wasn't available. Generate `InitialCreate` per "Running locally" above
   before first run.
3. **Booking/Payment-sourced notifications need an Auth Service endpoint
   that doesn't exist yet** — `GET /api/v1/users/{id}/contact` (or
   equivalent). See architecture doc §5 for the full explanation and why
   this wasn't added silently.
4. **Idempotency-Key cache is in-process memory**, not Redis-backed —
   fine for a single instance, breaks across replicas. See
   `Api/Middleware/IdempotencyMiddleware.cs`.
5. **Oracle and MongoDB are not wired** as database providers — see
   architecture doc §4 for why, and the extension point if needed.
6. **JMeter/NBomber load-test scenarios were not ported**, only k6 — see
   `tests/load/README.md`.
7. **FCM push and Twilio/SMS integrations are real, complete client code**
   but were obviously never exercised against live Firebase/Twilio
   credentials in this sandbox (no network). Sanity-check against a real
   sandbox/test account before production use.

## Further reading

- [Architecture](../../docs/architecture/notification-service-architecture.md) — design rationale, the recipient-resolution gap, retry/outbox design
- [Event catalog](../../docs/events/Event_Catalog.md) — events this service publishes and consumes
