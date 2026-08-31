using System.Text;
using Microsoft.Extensions.Options;
using Platform.SharedKernel.Correlation;
using RabbitMQ.Client;

namespace AuthService.Infrastructure.Messaging;

/// <summary>Publishes to a durable topic exchange. See BookingService's equivalent for the threading caveat (single channel, sequential publish from OutboxProcessor only).</summary>
public sealed class RabbitMqPublisher : IMessageBusPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly Lazy<IConnection> _connection;
    private readonly Lazy<IModel> _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        _connection = new Lazy<IConnection>(() =>
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                DispatchConsumersAsync = true
            };
            return factory.CreateConnection("auth-service");
        });

        _channel = new Lazy<IModel>(() =>
        {
            var channel = _connection.Value.CreateModel();
            channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);
            return channel;
        });
    }

    public Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = _channel.Value.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        // Carry the ambient correlation id onto the message when one exists
        // (M0). For the outbox background path this is currently null — durable
        // outbox correlation needs an outbox CorrelationId column, deferred to
        // M2/M9 (see docs/programmers-guide/correlation-id.md).
        var correlationId = CorrelationContext.Current;
        if (!string.IsNullOrEmpty(correlationId))
            properties.CorrelationId = correlationId;

        _channel.Value.BasicPublish(_options.Exchange, routingKey, properties, body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_channel.IsValueCreated) _channel.Value.Dispose();
        if (_connection.IsValueCreated) _connection.Value.Dispose();
    }
}
