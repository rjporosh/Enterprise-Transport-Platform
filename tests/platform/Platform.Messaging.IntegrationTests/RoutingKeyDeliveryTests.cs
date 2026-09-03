using System.Text;
using System.Text.Json;
using BookingService.Domain.Events;
using FluentAssertions;
using Platform.Contracts.Messaging;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Platform.Messaging.IntegrationTests;

/// <summary>
/// Proves the M0 routing-key fix end to end against a real broker:
/// resolve the key the way every OutboxProcessor now does, publish the real
/// serialized domain event, and assert it lands on a queue bound exactly the
/// way NotificationService's consumer binds (P0-4).
///
/// Requires Docker. If Docker is unavailable the whole class is skipped
/// (never silently "passes").
/// </summary>
[Trait("Category", "Integration")]
public sealed class RoutingKeyDeliveryTests : IAsyncLifetime
{
    private const string Exchange = "booking.events";
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    private IConnection _connection = null!;

    public async Task InitializeAsync()
    {
        await _rabbit.StartAsync();
        var factory = new ConnectionFactory { Uri = new Uri(_rabbit.GetConnectionString()) };
        _connection = factory.CreateConnection("platform-messaging-tests");
    }

    public async Task DisposeAsync()
    {
        _connection?.Dispose();
        await _rabbit.DisposeAsync();
    }

    [Fact]
    public void Resolver_produces_the_key_the_consumer_binds_to()
    {
        var storedEventType = typeof(BookingConfirmedDomainEvent).AssemblyQualifiedName!;

        var key = IntegrationEventRoutingKeys.Resolve(storedEventType, "booking");

        key.Should().Be(EventTypes.BookingConfirmed);
        key.Should().Be("booking.confirmed");
    }

    [Fact]
    public async Task Published_booking_confirmed_event_is_delivered_with_the_exact_routing_key()
    {
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(Exchange, ExchangeType.Topic, durable: true);

        // Bind exactly as NotificationService's consumer does.
        var queue = channel.QueueDeclare(queue: "test.notification.upstream", durable: true, exclusive: false, autoDelete: false).QueueName;
        channel.QueueBind(queue, Exchange, EventTypes.BookingConfirmed);

        var domainEvent = new BookingConfirmedDomainEvent(
            BookingId: Guid.NewGuid(),
            TripId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            PaymentId: Guid.NewGuid(),
            OperatorId: Guid.NewGuid(),
            CustomerEmail: "customer@example.com",
            CustomerName: "Test Customer",
            CustomerPhone: null,
            OriginCity: "Dhaka",
            DestinationCity: "Chattogram",
            DepartureUtc: DateTimeOffset.UtcNow.AddHours(4),
            ArrivalUtc: DateTimeOffset.UtcNow.AddHours(10),
            BusPlateNumber: "DHK-METRO-11-2345",
            BusType: "AC Sleeper",
            SeatNumbers: new[] { "1A", "1B" },
            PassengerNames: new[] { "Test Customer", "Guest" },
            TotalAmount: 1600m,
            Currency: "BDT");
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        var routingKey = IntegrationEventRoutingKeys.Resolve(
            typeof(BookingConfirmedDomainEvent).AssemblyQualifiedName!, "booking");

        Publish(channel, routingKey, payload, correlationId: "corr-abc123def");

        var received = WaitForMessage(channel, queue);

        received.Should().NotBeNull();
        received!.RoutingKey.Should().Be("booking.confirmed");
        received.CorrelationId.Should().Be("corr-abc123def");

        using var doc = JsonDocument.Parse(received.Body);
        doc.RootElement.GetProperty("BookingId").GetGuid().Should().Be(domainEvent.BookingId);
        doc.RootElement.GetProperty("CustomerId").GetGuid().Should().Be(domainEvent.CustomerId);
    }

    [Fact]
    public async Task A_routing_key_no_consumer_binds_to_is_not_delivered()
    {
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(Exchange, ExchangeType.Topic, durable: true);
        var queue = channel.QueueDeclare(queue: "test.only.confirmed", durable: true, exclusive: false, autoDelete: false).QueueName;
        channel.QueueBind(queue, Exchange, EventTypes.BookingConfirmed);

        // The OLD buggy key ("booking.booking.confirmed") would NOT match this
        // binding — proving why the P0-4 bug silently dropped notifications.
        Publish(channel, "booking.booking.confirmed", "{}", null);

        var received = WaitForMessage(channel, queue, attempts: 8);
        received.Should().BeNull("the double-prefixed key must not match a 'booking.confirmed' binding");
    }

    [Fact]
    public async Task Replaying_the_same_event_delivers_it_again_at_the_broker_level()
    {
        // Consumer-side idempotency / inbox de-duplication is a LATER milestone
        // (M7 for NotificationService). At the broker level, an at-least-once
        // outbox legitimately re-delivers on retry — this test pins that
        // expectation so a future inbox change is a conscious decision.
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(Exchange, ExchangeType.Topic, durable: true);
        var queue = channel.QueueDeclare(queue: "test.replay", durable: true, exclusive: false, autoDelete: false).QueueName;
        channel.QueueBind(queue, Exchange, EventTypes.BookingConfirmed);

        var eventId = Guid.NewGuid();
        var payload = $$"""{"EventId":"{{eventId}}","BookingId":"{{Guid.NewGuid()}}"}""";

        Publish(channel, EventTypes.BookingConfirmed, payload, null);
        Publish(channel, EventTypes.BookingConfirmed, payload, null);

        var first = WaitForMessage(channel, queue);
        var second = WaitForMessage(channel, queue);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        JsonDocument.Parse(first!.Body).RootElement.GetProperty("EventId").GetGuid().Should().Be(eventId);
        JsonDocument.Parse(second!.Body).RootElement.GetProperty("EventId").GetGuid().Should().Be(eventId);
    }

    private static void Publish(IModel channel, string routingKey, string payload, string? correlationId)
    {
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        if (!string.IsNullOrEmpty(correlationId))
            props.CorrelationId = correlationId;

        channel.BasicPublish(Exchange, routingKey, props, Encoding.UTF8.GetBytes(payload));
    }

    private static ReceivedMessage? WaitForMessage(IModel channel, string queue, int attempts = 25)
    {
        for (var i = 0; i < attempts; i++)
        {
            var result = channel.BasicGet(queue, autoAck: true);
            if (result is not null)
            {
                return new ReceivedMessage(
                    result.RoutingKey,
                    result.BasicProperties?.CorrelationId,
                    Encoding.UTF8.GetString(result.Body.ToArray()));
            }

            Thread.Sleep(200);
        }

        return null;
    }

    private sealed record ReceivedMessage(string RoutingKey, string? CorrelationId, string Body);
}
