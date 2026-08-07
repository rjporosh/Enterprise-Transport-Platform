namespace NotificationService.Infrastructure.Messaging;

public interface IMessageBusPublisher
{
    Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default);
}
