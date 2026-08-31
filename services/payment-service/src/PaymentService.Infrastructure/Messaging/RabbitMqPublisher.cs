using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.SharedKernel.Correlation;
using RabbitMQ.Client;

namespace PaymentService.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessageBusPublisher, IDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly RabbitMqOptions _options;
    private readonly Lazy<IConnection> _connection;
    private readonly Lazy<IModel> _channel;
    private bool _disposed;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        _options = options.Value;

        _connection = new Lazy<IConnection>(() =>
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                if (_options.UseSsl)
                    factory.Ssl = new SslOption { Enabled = true };

                return factory.CreateConnection();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection failed. Event publishing will be unavailable.");
                return null!;
            }
        });

        _channel = new Lazy<IModel>(() =>
        {
            if (_connection.Value == null!)
                return null!;

            var channel = _connection.Value.CreateModel();
            channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);
            return channel;
        });
    }

    public async Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default)
    {
        if (_connection.IsValueCreated && _connection.Value == null!)
            return;

        if (_channel.IsValueCreated && _channel.Value == null!)
            return;

        try
        {
            var body = System.Text.Encoding.UTF8.GetBytes(payload);
            var properties = _channel.Value.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Carry the ambient correlation id onto the message when one exists
            // (M0). Null on the outbox background path — durable outbox
            // correlation needs an outbox CorrelationId column, deferred to
            // M2/M9 (see docs/programmers-guide/correlation-id.md).
            var correlationId = CorrelationContext.Current;
            if (!string.IsNullOrEmpty(correlationId))
                properties.CorrelationId = correlationId;

            _channel.Value.BasicPublish(
                exchange: _options.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published event {RoutingKey} to exchange {Exchange}", routingKey, _options.Exchange);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {RoutingKey}", routingKey);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_channel.IsValueCreated && _channel.Value != null!)
                _channel.Value.Dispose();

            if (_connection.IsValueCreated && _connection.Value != null!)
                _connection.Value.Dispose();

            _disposed = true;
        }
    }
}
