# Event Catalog

All events are published to a durable, topic-exchange RabbitMQ setup via
each service's own transactional outbox (see `Outbox.md`) — never published
directly from a request handler. Routing key convention: `<service>.<noun>.<verb-past-tense>`
where practical (some earlier events predate the convention and are
`<service>.<verb-past-tense>` — noted below).

## Auth Service — exchange `auth.events`

| Routing key | Event | Payload | Consumed by |
|---|---|---|---|
| `auth.user.registered` | `UserRegisteredDomainEvent` | `UserId, Email, FirstName, LastName` | Notification Service (welcome email) |
| `auth.password.changed` | `PasswordChangedDomainEvent` | `UserId, Email` | Notification Service (security alert email) |
| `auth.user.locked-out` | `UserLockedOutDomainEvent` | `UserId, Email, LockedUntilUtc` | Notification Service (security alert email) |

`UserLoggedInDomainEvent` (`UserId, Email, Ip`) is raised but not currently
published to an outbound routing key intended for cross-service consumption
— see `AuthService.Domain/Events/UserLoggedInDomainEvent.cs`.

## Booking Service — exchange `booking.events`

| Routing key | Event | Payload | Consumed by |
|---|---|---|---|
| `booking.created` | `BookingCreatedDomainEvent` | `BookingId, TripId, CustomerId, TotalAmount, Currency, SeatNumbers` | Payment Service (start payment intent); Notification Service (booking-held confirmation — **recipient-resolution gap, see below**) |
| `booking.confirmed` | `BookingConfirmedDomainEvent` | `BookingId, TripId, CustomerId` | Notification Service (booking-confirmed email — **recipient-resolution gap**) |
| `booking.cancelled` | `BookingCancelledDomainEvent` | `BookingId, TripId, Reason` | Notification Service (cancellation email — **recipient-resolution gap**) |

## Payment Service — exchange `payment.events`

Payment Service has no implementation yet (see repo `NEXT_STEP.md` / `ROADMAP.md`
at the time of writing). Notification Service's `RabbitMq:UpstreamBindings`
config and `NotificationEventConsumer.RoutingKeyMap` are pre-wired for
`payment.completed` and `payment.failed` so no further Notification Service
change is needed once Payment Service starts publishing them — only the
config binding needs the routing key's actual field names confirmed once
that event contract exists.

## Notification Service — exchange `notification.events`

| Routing key | Event | Payload | Consumed by |
|---|---|---|---|
| `notification.created` | `NotificationCreatedDomainEvent` | `NotificationId, Channel, Recipient, Priority` | (none yet — available for an admin dashboard/analytics consumer) |
| `notification.sent` | `NotificationSentDomainEvent` | `NotificationId, Channel, Recipient, SentAtUtc` | (none yet) |
| `notification.delivered` | `NotificationDeliveredDomainEvent` | `NotificationId, Channel, DeliveredAtUtc` | (none yet — raised only for providers with delivery-receipt support) |
| `notification.failed` | `NotificationFailedDomainEvent` | `NotificationId, Channel, Recipient, Reason, AttemptNumber, WillRetry` | (none yet) |
| `notification.cancelled` | `NotificationCancelledDomainEvent` | `NotificationId, Reason` | (none yet) |
| `notification.dead-lettered` | `NotificationDeadLetteredDomainEvent` | `NotificationId, LastError, TotalAttempts` | (none yet — intended for an on-call/alerting integration) |

## Known gap: Booking/Payment events don't carry a recipient contact address

`BookingCreatedDomainEvent`/`BookingConfirmedDomainEvent`/`BookingCancelledDomainEvent`
carry only `CustomerId` (a `Guid`), not an email or phone number.
Notification Service's `NotificationEventConsumer` handles this by calling
out to `IUserDirectoryClient` to resolve `CustomerId` → contact info — but
that client is wired against an Auth Service endpoint
(`GET /api/v1/users/{id}/contact`) that doesn't exist yet (only
`GET /api/v1/auth/me`, self-lookup, does). Until it's added, booking-sourced
notifications fail gracefully (logged, message acked-and-dropped) rather
than crashing the consumer. See
`docs/architecture/notification-service-architecture.md` §5 for the full
writeup and rationale for not adding that endpoint silently as part of the
Notification Service delivery.
