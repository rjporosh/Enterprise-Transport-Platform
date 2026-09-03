using System.Text;
using BookingService.Infrastructure.Persistence;
using BookingService.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookingService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Base for booking-service's inbound RabbitMQ consumers. Handles the
/// connection lifecycle, graceful degradation when the broker is down at
/// startup, per-message inbox de-duplication, and single-retry poison-message
/// handling. Subclasses declare their bindings and process one event.
/// </summary>
public abstract class RabbitMqEventConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger _logger;

    protected readonly IServiceScopeFactory ScopeFactory;
    private IConnection? _connection;
    private IModel? _channel;

    protected RabbitMqEventConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _options = options.Value;
        ScopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Stable name used as the inbox <c>Consumer</c> discriminator and the durable queue name.</summary>
    protected abstract string ConsumerName { get; }

    /// <summary>(exchange, routing-key pattern) pairs this consumer binds to.</summary>
    protected abstract IReadOnlyCollection<(string Exchange, string RoutingKey)> Bindings { get; }

    /// <summary>Handle one event. <paramref name="body"/> is the raw JSON payload. Throw to trigger a single requeue.</summary>
    protected abstract Task HandleAsync(string routingKey, string body, IServiceScope scope, CancellationToken cancellationToken);

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                DispatchConsumersAsync = true
            };
            _connection = factory.CreateConnection($"booking-service:{ConsumerName}");
            _channel = _connection.CreateModel();

            var queueName = $"booking-service.{ConsumerName}";
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

            foreach (var (exchange, routingKey) in Bindings)
            {
                _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
                _channel.QueueBind(queueName, exchange, routingKey);
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnReceivedAsync;
            _channel.BasicConsume(queueName, autoAck: false, consumer);

            _logger.LogInformation("{Consumer} subscribed to {Count} binding(s).", ConsumerName, Bindings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{Consumer} failed to start. Root cause: RabbitMQ unreachable at {Host}:{Port}. " +
                "Possible solution: start RabbitMQ (docker compose up -d rabbitmq) and verify RabbitMq:* config. " +
                "The booking API still serves reads/writes; this consumer stays down until the broker returns.",
                ConsumerName, _options.HostName, _options.Port);
        }

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var routingKey = args.RoutingKey;
        var body = Encoding.UTF8.GetString(args.Body.ToArray());

        // Prefer the broker-supplied message id; fall back to a deterministic
        // hash of the payload so a producer that doesn't set one still dedups.
        var messageId = Guid.TryParse(args.BasicProperties?.MessageId, out var mid)
            ? mid
            : DeterministicGuid(body);

        try
        {
            using var scope = ScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

            var alreadyHandled = await db.InboxMessages
                .AsNoTracking()
                .AnyAsync(m => m.Id == messageId && m.Consumer == ConsumerName);

            if (alreadyHandled)
            {
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            await HandleAsync(routingKey, body, scope, CancellationToken.None);

            db.InboxMessages.Add(new InboxMessage
            {
                Id = messageId,
                Consumer = ConsumerName,
                RoutingKey = routingKey,
                ReceivedAtUtc = DateTimeOffset.UtcNow,
                ProcessedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{Consumer} failed to process '{RoutingKey}'. Payload: {Payload}. Requeue={Requeue}.",
                ConsumerName, routingKey, body, !args.Redelivered);
            _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: !args.Redelivered);
        }
    }

    private static Guid DeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
