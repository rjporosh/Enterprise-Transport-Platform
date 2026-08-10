# Consuming Events

## Upstream Events

`NotificationEventConsumer` binds to upstream service exchanges and turns their domain events into outbound notifications.

### Routing Map

| Routing Key | Template Key | Channel |
|---|---|---|
| `auth.user.registered` | `auth.welcome` | Email |
| `auth.password.changed` | `auth.password-changed` | Email |
| `auth.user.locked-out` | `auth.account-locked` | Email |
| `booking.created` | `booking.held` | Email |
| `booking.confirmed` | `booking.confirmed` | Email |
| `booking.cancelled` | `booking.cancelled` | Email |
| `payment.completed` | `payment.receipt` | Email |
| `payment.failed` | `payment.failed` | Email |

### Recipient Resolution

The consumer extracts `UserId`/`CustomerId` from the event payload, then:
1. Checks for inline `Email`/`PhoneNumber` fields
2. Falls back to `IUserDirectoryClient.ResolveContactAsync(recipientId)` (requires Auth Service endpoint)

If no recipient can be resolved, the message is ack'd and dropped (logged as warning).

### Graceful Degradation

- Unmapped routing key → ack and drop
- Transient failure → requeue once
- Poison message → ack and drop after requeue

## Inbox Pattern

When consuming events that modify state, use deduplication. The `OutboxProcessor` handles deduplication at the outbox level. For inbound events, ensure the handler is idempotent by checking if the notification was already created.

## Idempotency

Use `Idempotency-Key` on POST/PUT/PATCH requests. The middleware caches responses for 24 hours and replays them for duplicate keys.
