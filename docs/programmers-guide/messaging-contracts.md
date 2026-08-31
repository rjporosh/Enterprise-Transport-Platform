# Messaging Contracts & Routing Keys

**Status:** implemented in milestone **M0**.
**Project:** `shared/contracts` (`Platform.Contracts`)
**Tests:** `shared/contracts/tests/Platform.Contracts.Tests` (226 tests) +
`tests/platform/Platform.Messaging.IntegrationTests` (real RabbitMQ, 4 tests).

---

## The problem this fixes (P0-4)

Each service used to derive its RabbitMQ routing key by string-munging the
stored CLR type name in the outbox row:

| Service | Old result | Should be |
|---------|------------|-----------|
| booking | `booking.booking.confirmed` (double prefix) | `booking.confirmed` |
| bus | `bus.bus.registered` | `bus.registered` |
| route | `route.route.created` | `route.created` |
| payment | `payment.<culture=neutral, PublicKeyToken=null>` (split an AssemblyQualifiedName on `.`) | `payment.succeeded` |
| auth | `auth.user.registered` (correct — only by luck, its event classes aren't service-prefixed) | unchanged |

The notification consumer binds to `booking.confirmed`, `payment.succeeded`, …,
so **booking-confirmation and payment-receipt notifications could never be
delivered**.

---

## The contract

`Platform.Contracts.Messaging.EventTypes` holds every routing key as a `const`.
These are a **published contract** — a value here never changes once shipped;
adding an event means adding a constant + a registry entry.

```
auth.events:         auth.user.registered, auth.password.changed, auth.user.locked.out, ...
booking.events:      booking.created, booking.confirmed, booking.cancelled
payment.events:      payment.created, payment.processing, payment.succeeded, payment.failed,
                     payment.cancelled, payment.refunded
bus.events:          bus.registered, bus.details.updated, bus.status.changed, ...
route.events:        route.created, route.updated, route.deleted, route.status.changed,
                     route.stop.created, route.stop.updated, route.schedule.created
notification.events: notification.created, notification.sent, notification.delivered, ...
ticket.events:       ticket.issued, ticket.cancelled, ticket.reissued   (reserved — M6)
```

Exchange per service: `<service>.events` (durable topic exchange).

Word separator is `.` (AMQP topic convention). First segment is always the
owning service.

---

## How a service resolves a routing key

Every `OutboxProcessor` (and payment's `FailedWebhookRetryJob`) now calls:

```csharp
IntegrationEventRoutingKeys.TryResolve(message.EventType, "<service>", out var routingKey, out var fromRegistry);
if (!fromRegistry)
    _logger.LogWarning("... event {EventType} not in the registry; used fallback {RoutingKey}", ...);
await publisher.PublishAsync(routingKey, message.Payload, ct);
```

`IntegrationEventRoutingKeys` (in `Platform.Contracts.Messaging`):

1. Extracts the short CLR type name — handles an `AssemblyQualifiedName`, a
   `Namespace.FullName`, or a bare name.
2. Looks it up in `EventTypeRegistry` (the explicit contract). If found → done.
3. Otherwise builds a **deterministic fallback**: `<service>.<dotted-name>`
   with the suffix (`DomainEvent`/`IntegrationEvent`/`Event`) stripped and
   **no double prefix** (`BookingConfirmed` → `booking.confirmed`, already
   prefixed, returned as-is). `fromRegistry` is `false` so the caller logs it.

The fallback guarantees a sane, language-neutral key even for an event nobody
registered yet — it never produces `x.x.y` or leaks assembly metadata.

---

## Adding a new event

1. Add the routing-key constant to `EventTypes`.
2. Add `["MyNewDomainEvent"] = EventTypes.MyNew` to `EventTypeRegistry`.
3. (Optional but recommended) add a versioned contract record to
   `shared/contracts/Events/IntegrationEvents.cs` documenting the JSON shape.
4. The contract test suite will fail if a domain event exists without a registry
   entry — run `dotnet test shared/Platform.Shared.sln`.

---

## Wire payload

Today each service serialises **its own domain-event record** to the outbox
`Payload` (System.Text.Json, PascalCase). The `Platform.Contracts.Events.*V1`
records document the field names/types that cross the service boundary so
polyglot consumers (Node/Java/Python — future) have a canonical schema. The
contract test asserts the live domain-event JSON deserialises into the matching
`V1` record.

**Not changed in M0:** the payload format itself, or the `OutboxEventPublisher`
(it still stores the AssemblyQualifiedName as the event-type token — the
resolver handles that fine).

---

## Correlation on messages

`RabbitMqPublisher.PublishAsync` now sets `IBasicProperties.CorrelationId` from
`Platform.SharedKernel.Correlation.CorrelationContext.Current` when an ambient
value exists. See `correlation-id.md` — full end-to-end correlation **through the
outbox** needs a persisted `CorrelationId` column and is deferred to M2/M9.

---

## Consumer-side de-duplication

The notification consumer has **no inbox / de-dup table** — a redelivered event
creates a duplicate notification. This is a known gap, tracked for milestone
**M7**. `Platform.Messaging.IntegrationTests` pins the current at-least-once
broker behaviour so the M7 change is a conscious decision.
