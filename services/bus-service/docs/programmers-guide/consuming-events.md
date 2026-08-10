# Programmer's Guide — Consuming Events

Bus Service publishes the following domain events to the `bus.events` exchange on RabbitMQ:

| Event | Routing Key | When Raised |
|---|---|---|
| `BusRegisteredDomainEvent` | `bus.registered` | New bus added to fleet |
| `BusDetailsUpdatedDomainEvent` | `bus.details.updated` | Bus type, seats, depot, or fleet details changed |
| `BusStatusChangedDomainEvent` | `bus.status.changed` | Bus transitions between Active / UnderMaintenance / Retired |
| `BusSoftDeletedDomainEvent` | `bus.soft.deleted` | Bus soft-deleted |
| `BusRestoredDomainEvent` | `bus.restored` | Soft-deleted bus restored |

## Payload Shape

All events share a common envelope:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "occurredOnUtc": "2026-08-10T12:00:00Z",
  "eventType": "BusRegisteredDomainEvent",
  "payload": {
    "busId": "...",
    "operatorId": "...",
    "plateNumber": "DHA-1234",
    ...
  }
}
```

## Subscribing from Another Service

Using `RabbitMQ.Client`:

```csharp
var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare("bus.events", ExchangeType.Topic, durable: true);
var queue = channel.QueueDeclare("booking.bus.sync", durable: true);
channel.QueueBind(queue, "bus.events", "bus.#");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
  var body = ea.Body.ToArray();
  var message = Encoding.UTF8.GetString(body);
  Console.WriteLine($"Received: {message}");
};
channel.BasicConsume(queue, autoAck: true, consumer);
```

## Outbox Guarantees

Events are persisted to `outbox_messages` inside the same database transaction as the state change. `OutboxProcessor` (a background hosted service) polls for unprocessed messages and publishes them to RabbitMQ with retry logic. This guarantees **at-least-once delivery** without distributed transactions.
