# Notification Service

Email/SMS/Push delivery, templates, retry/outbox, and event-driven
notifications for the Enterprise Transport Platform. Built with .NET 10,
Clean Architecture, and CQRS (MediatR) — see
[`docs/architecture.md`](./docs/architecture.md) for the full design rationale.

## What's here

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `NotificationService.Domain` | `Notification` (send/retry/cancel state machine), `NotificationTemplate`, `RecipientPreference`, `NotificationLog` — zero framework deps |
| Application | `NotificationService.Application` | CQRS: Send/Get/Cancel/Retry/Delete notifications; Create/Update/Get/Delete templates; Get/Update recipient preferences |
| Infrastructure | `NotificationService.Infrastructure` | EF Core (Postgres/SqlServer/MySql switch), outbox + RabbitMQ (publish and an upstream-event consumer), SMTP (MailKit) / Twilio+GenericHttp SMS / FCM HTTP v1 push, Scriban templates, Polly retry, Quartz jobs, resx localization (en, bn) |
| Api | `NotificationService.Api` | REST + gRPC endpoints, JWT bearer auth, rate limiting, native OpenAPI+Scalar, health checks, OpenTelemetry+Prometheus |
| Tests | `NotificationService.UnitTests`, `NotificationService.IntegrationTests` | Handler/state-machine unit tests (EF InMemory), Testcontainers-based API tests |
| Load tests | `tests/load-test/` | k6, NBomber, JMeter — see `tests/load-test/README.md` |

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

See [`tests/load-test/README.md`](tests/load-test/README.md) for load tests.

## Load Tests

See [`tests/load-test/README.md`](tests/load-test/README.md) for k6, NBomber, and JMeter instructions.

## Documentation

- [`docs/architecture.md`](./docs/architecture.md) — design rationale
- [`docs/db-schema.md`](./docs/db-schema.md) — database schema and indexes
- [`docs/programmers-guide/`](./docs/programmers-guide/) — developer guides
- [`docs/diagrams/c4/`](./docs/diagrams/c4/) — C4 architecture diagrams
- [`docs/scripts/postman/`](./docs/scripts/postman/) — Postman collection

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

1. **Auth Service `GET /api/v1/users/{id}/contact` endpoint does not exist yet** — Booking/Payment event recipient resolution falls back to inline contact fields or acks-and-drops.
2. **Idempotency-Key cache is in-memory** (ConcurrentDictionary) — fine for a single instance, breaks across replicas. Redis backing needed for production multi-instance.
3. **Oracle and MongoDB database providers are not wired** — requires separate architecture decision.
4. **FCM push and Twilio/SMS integrations are real, complete client code** but require real credentials to deliver.
5. **Search uses `ToLower().Contains()`** instead of `EF.Functions.ILike` for cross-provider compatibility — no case-insensitive index on PostgreSQL.

## Further reading

- [Architecture](./docs/architecture.md) — design rationale, recipient-resolution gap, retry/outbox design
- [Database Schema](./docs/db-schema.md) — entities, tables, indexes, constraints
- [Programmer Guide](./docs/programmers-guide/) — CRUD, CQRS, validation, migrations, Quartz, testing
- [Event Catalog](../../docs/events/Event_Catalog.md) — events this service publishes and consumes
- [Postman Collection](./docs/scripts/postman/notification-service.postman-collection.json)
