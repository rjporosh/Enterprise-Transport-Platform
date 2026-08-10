# Background Workers

## Types

The service uses two types of background workers:

1. **Quartz Jobs** — scheduled, time-based work (dispatch, recovery)
2. **BackgroundService** — long-running processes (outbox processor, RabbitMQ consumer)

## OutboxProcessor (BackgroundService)

Polls the `outbox_messages` table every 5 seconds and relays unprocessed events to RabbitMQ.

```csharp
public sealed class OutboxProcessor : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxRetries = 5;
    // ...
}
```

## NotificationEventConsumer (BackgroundService)

Subscribes to upstream RabbitMQ exchanges and transforms events into notifications. Gracefully handles startup failure (logs error, doesn't crash API).

## Best Practices

- Always resolve scoped services via `IServiceScopeFactory`
- Respect `CancellationToken` from the hosting environment
- Log structured information including job name, trigger name, correlation ID
- Make jobs idempotent — assume they may run more than once
- Handle failures gracefully — log and continue, don't crash the process
