# Programmer's Guide — Background Workers

## OutboxProcessor

`OutboxProcessor` is a hosted background service that polls `outbox_messages` for unprocessed entries and publishes them to RabbitMQ.

```csharp
services.AddHostedService<OutboxProcessor>();
```

### Behavior

- Polls every 500ms (configurable).
- Fetches up to 50 unprocessed messages per batch.
- Publishes each message to the configured exchange (`bus.events`) with the event type as the routing key.
- Marks messages as `ProcessedOnUtc` on success; increments `RetryCount` and logs the error on failure.
- Stops gracefully on shutdown.

### Configuration

```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "Exchange": "bus.events"
  }
}
```

## QueryLogWriterBackgroundService

`QueryLogWriterBackgroundService` flushes SQL query logs to disk every 2 seconds.

```csharp
services.AddHostedService<QueryLogWriterBackgroundService>();
```

Enable in `appsettings.Development.json`:

```json
{
  "Logging": {
    "EnableQueryLogging": true
  }
}
```

Logs are written to `logs/query-log-<dd-MM-yyyy>.txt`.
