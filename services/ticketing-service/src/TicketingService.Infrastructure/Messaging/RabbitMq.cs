using System.Text;
using Microsoft.Extensions.Options;
using Platform.SharedKernel.Correlation;
using RabbitMQ.Client;

namespace TicketingService.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "ticket.events";
}

public interface IMessageBusPublisher
{
    Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default);
}

public sealed class RabbitMqPublisher : IMessageBusPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly Lazy<IConnection> _connection;
    private readonly Lazy<IModel> _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
        _connection = new Lazy<IConnection>(() => new ConnectionFactory
        {
            HostName = _options.HostName, Port = _options.Port,
            UserName = _options.UserName, Password = _options.Password,
            DispatchConsumersAsync = true
        }.CreateConnection("ticketing-service"));

        _channel = new Lazy<IModel>(() =>
        {
            var ch = _connection.Value.CreateModel();
            ch.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);
            return ch;
        });
    }

    public Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default)
    {
        var props = _channel.Value.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = Guid.NewGuid().ToString();
        var correlationId = CorrelationContext.Current;
        if (!string.IsNullOrEmpty(correlationId)) props.CorrelationId = correlationId;

        _channel.Value.BasicPublish(_options.Exchange, routingKey, props, Encoding.UTF8.GetBytes(payload));
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_channel.IsValueCreated) _channel.Value.Dispose();
        if (_connection.IsValueCreated) _connection.Value.Dispose();
    }
}
