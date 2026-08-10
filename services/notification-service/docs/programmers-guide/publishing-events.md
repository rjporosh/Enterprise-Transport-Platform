# Publishing Events

## Outbox Pattern

Events are published through the outbox pattern to ensure consistency between database state and message publication.

### Flow

```
Handler
  ↓
_notificationDbContext.Notifications.Add(notification)
  ↓
foreach (domainEvent in notification.DomainEvents)
    await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken)
  ↓
await _dbContext.SaveChangesAsync(cancellationToken)
  ↓
[OutboxMessage rows are now in the same DB transaction]
  ↓
OutboxProcessor (BackgroundService) polls and relays to RabbitMQ
```

### IEventPublisher

```csharp
public interface IEventPublisher
{
    Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
```

### Implementation

`OutboxEventPublisher` stages an `OutboxMessage` on the change tracker. It does NOT call `SaveChangesAsync` — the handler commits it in the same transaction.

### Routing

`OutboxProcessor` derives the RabbitMQ routing key from the event type name:
- `NotificationSentDomainEvent` → `notification.sent`
- `NotificationFailedDomainEvent` → `notification.failed`
- `NotificationDeadLetteredDomainEvent` → `notification.dead-lettered`
- `NotificationCancelledDomainEvent` → `notification.cancelled`
- `NotificationCreatedDomainEvent` → `notification.created`
- `NotificationDeliveredDomainEvent` → `notification.delivered`

## Retry

Failed publishes increment `RetryCount` and are retried up to 5 times. After exhaustion, the message stays in the outbox for manual triage.

## RabbitMQ Topology

- Exchange: `notification.events` (topic, durable)
- Messages: persistent, with `content_type` and `correlation_id` headers
