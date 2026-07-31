namespace BookingService.Infrastructure.Messaging;

/// <summary>Thin abstraction over the message broker so OutboxProcessor stays testable.</summary>
public interface IMessageBusPublisher
{
    Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default);
}
