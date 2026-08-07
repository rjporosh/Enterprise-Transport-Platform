# Notification Service — Architecture

Email/SMS/Push delivery, templates, retry/outbox, and event-driven
notifications for the Enterprise Transport Platform. Built with .NET 10,
Clean Architecture, and CQRS (MediatR) — same shape as Auth/Booking Service.

> **Build status note**: built in a sandbox with no .NET SDK and no network
> access — every file is hand-reviewed for correctness against the actual
> conventions already established in `AuthService`/`BookingService` (verified
> by reading their source directly, not from memory), but **not compiled**.
> Run a clean `dotnet build` / `dotnet test` before deploying — see this
> service's `README.md`, "Known limitations".

## 1. Layering

Identical to `BookingService`/`AuthService`: `Domain` has zero framework
dependencies; `Application` depends only on `Domain` + MediatR/FluentValidation
+ EF Core's `DbSet<T>` abstraction (via `INotificationDbContext`, never a
concrete provider); `Infrastructure` implements every `Application` interface
against real EF Core/RabbitMQ/SMTP/Twilio/FCM; `Api` wires it all together and
exposes REST + gRPC.

## 2. The Notification state machine

```
Pending ──┐
Scheduled ┼─► Sending ─► Sent ─► Delivered (optional, provider-dependent)
Retrying ─┘        │
                    └─► Failed ─┬─► Retrying (loop, exponential backoff, capped 60min)
                                └─► DeadLettered (retries exhausted)

Pending/Scheduled/Retrying ─► Cancelled (operator/caller action, before send)
Any status ─► soft-deleted (hidden from listings, row retained for audit)
```

See `Domain/Entities/Notification.cs` for the enforcing code — every
transition is a method that throws `InvalidNotificationStateException` if
called from the wrong status, so an invalid transition is a compile-time-
reachable, unit-tested bug, not a possible-but-unenforced state.

## 3. Why the API never calls a channel provider directly

`SendNotificationHandler` inserts a `Pending`/`Scheduled` row and enqueues its
`NotificationCreatedDomainEvent` — it does not call `IEmailSender` etc. The
actual send happens later, off the request thread, in
`NotificationDispatchJob` (a Quartz job polling every 10s). This means:

- The write API's latency is DB-insert-bound, not SMTP/SMS-gateway-bound —
  see `tests/load/k6/send-notification-load-test.js`, which asserts p95 &lt;
  300ms specifically because of this.
- Immediate sends, scheduled-for-later sends, and automatic retries all flow
  through **one** dispatch code path — no duplicated send logic to keep in
  sync between "the API path" and "the retry job".
- A provider outage degrades to a growing `Retrying` queue, never a failing
  API call.

`StuckNotificationRecoveryJob` (runs every 5 min) is the safety net for the
one edge case this design introduces: a process crash between
`MarkSending()` and `MarkSent()`/`MarkFailed()` would otherwise leave a row
stuck in `Sending` forever with nothing watching it.

## 4. Database portability

`Database:Provider` (Postgres | SqlServer | MySql) picks the EF Core provider
at startup — see `Infrastructure/DependencyInjection.cs`, `AddDatabase`.
Identical convention to `AuthService.Infrastructure`, including the same
caveat: **switching providers means regenerating migrations, not just
flipping a config value in production.**

**Oracle and MongoDB are deliberately not wired.** Oracle's EF Core provider
(`Oracle.EntityFrameworkCore.Core`) is dual-licensed/commercial and its exact
current licensing terms can't be verified without network access from this
sandbox — wiring it without being certain of the license would be
irresponsible. MongoDB is a document store, not a relational EF Core
provider — supporting it behind the same `INotificationDbContext` interface
would mean a second, parallel persistence implementation (no LINQ-to-SQL
translation, no relational transactions backing the outbox pattern's
atomicity guarantee), which is a genuine architecture decision, not a
one-line provider switch. Per this task's own rule ("stop and ask for
approval if a change would affect... overall architecture"), that decision
is flagged here rather than made silently.

**Search** (`GetNotificationsHandler`, `GetTemplatesHandler`) deliberately
uses `.ToLower().Contains()` rather than `EF.Functions.ILike` — the latter is
Npgsql-only and would throw under `Database:Provider=SqlServer|MySql`.
Trade-off: no case-insensitive index usage on Postgres at this table's
current expected volume; revisit with a trigram/full-text index if search
becomes a hot path.

## 5. Event consumption — the recipient-resolution gap

`NotificationEventConsumer` binds to Auth/Booking/Payment Service's own
topic exchanges and turns their domain events into notifications. This
works end-to-end today for **Auth Service's events**
(`UserRegisteredDomainEvent`, `PasswordChangedDomainEvent`,
`UserLockedOutDomainEvent`) because they already carry an `Email` field
inline.

**It does not yet work end-to-end for Booking/Payment Service's events**
(`BookingCreatedDomainEvent`, `BookingConfirmedDomainEvent`, etc.) —
inspecting `services/booking-service/src/BookingService.Domain/Events/`
shows they carry only a `Guid CustomerId`, no email/phone. `IUserDirectoryClient`
(`Infrastructure/Messaging/`) is written and wired against the endpoint shape
it needs — `GET {AuthServiceBaseUrl}/api/v1/users/{id}/contact` — but that
endpoint does not exist on Auth Service today (only `GET /api/v1/auth/me`,
self-lookup, exists — see `AuthService.Api/Endpoints/AuthEndpoints.cs`).

Until that endpoint is added, `NotificationEventConsumer` fails gracefully
for booking/payment-sourced events: it logs a structured warning/error and
acks-and-drops the message rather than crashing the consumer loop or
retry-looping forever on an unresolvable message. **Adding the endpoint to
Auth Service is a cross-service contract change and was intentionally not
made as part of this "Notification Service only" delivery** — see this
service's `README.md`, "Known limitations", for the exact follow-up.

## 6. Retry, backoff, and the two different "retry" concepts

Two independent retry mechanisms exist, at different layers:

1. **In-process/Polly** (`Infrastructure/Retry/ChannelRetryPolicyFactory`) —
   wraps a single channel-provider call (e.g. one SMTP attempt) with a few
   fast retries for transient failures (a dropped connection), configurable
   via `Retry:MaxAttempts`/`Retry:BaseDelayMilliseconds`.
2. **State-machine/domain** (`Notification.MarkFailed`) — the coarser
   "this whole dispatch attempt failed" retry: exponential backoff (1, 2, 4,
   8... capped at 60 minutes) across separate `NotificationDispatchJob` runs,
   up to `MaxRetryCount`, then `DeadLettered`.

They compose: a Polly retry exhausting still counts as one domain-level
"failed" attempt.

## 7. Localization

`ILocalizationService` (Application) / `ResourceLocalizationService`
(Infrastructure) resolves message keys via `System.Resources.ResourceManager`
against `.resx` files — English (`Messages.resx`, default/fallback) and
Bangla (`Messages.bn.resx`). Adding a third language is a new
`Messages.<culture>.resx` file, no code change — `ResourceManager` resolves
culture fallback automatically. `LocalizationMiddleware` (Api) resolves the
request's locale from `?lang=` then `Accept-Language`, defaulting to English.

This is separate from `RecipientPreference.Locale` / `Notification.Locale`,
which control the **content locale of the notification itself** (which
`.resx`-equivalent template row gets rendered — see
`NotificationTemplate` and `SendNotificationHandler.ResolveTemplateAsync`),
not the locale of an API response.

## 8. gRPC vs REST vs RabbitMQ — three ways in, on purpose

- **REST** (`Api/Endpoints/`) — the admin console and external/browser callers.
- **gRPC** (`Api/Grpc/`, `Api/Protos/notification.proto`) — Booking/Payment/
  Auth Service calling in-process with a send-accepted acknowledgement, when
  a caller needs to know the send was *queued* before it proceeds (e.g.
  blocking a "confirmation email queued" UI state) without the overhead of a
  full async round-trip through RabbitMQ.
- **RabbitMQ / `NotificationEventConsumer`** — fully decoupled, fire-and-forget:
  upstream services publish their own domain events and never need to know
  Notification Service exists at all.

All three ultimately call the same `SendNotificationCommand` handler — no
duplicated business logic between them.
