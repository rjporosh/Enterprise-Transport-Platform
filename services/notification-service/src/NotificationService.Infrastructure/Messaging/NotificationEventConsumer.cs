using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Features.Notifications.SendNotification;
using NotificationService.Domain.Enums;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Consumes domain events published by Auth/Booking/Payment Services'
/// own outbox processors and turns the relevant ones into outbound
/// notifications (CLAUDE.md, "Event Consumption").
///
/// Declares one durable queue bound to every configured UpstreamBinding
/// (RabbitMq:UpstreamBindings in appsettings) — each binding is
/// (exchange, routing-key pattern) on an upstream service's own topic
/// exchange (e.g. "auth.events" / "auth.user.*"). This service never
/// publishes to those exchanges, only binds a queue to them — a standard,
/// loosely-coupled "any number of subscribers" topology that doesn't
/// require Auth/Booking/Payment to know Notification Service exists.
///
/// Routing-key -&gt; template-key mapping is intentionally small and explicit
/// (not reflection-based "guess the template from the event type") so a new
/// event mapping is a one-line, reviewable addition — see RoutingKeyMap.
/// </summary>
public sealed class NotificationEventConsumer : BackgroundService
{
    private static readonly IReadOnlyDictionary<string, (string TemplateKey, NotificationChannel Channel)> RoutingKeyMap =
        new Dictionary<string, (string, NotificationChannel)>
        {
            ["auth.user.registered"] = ("auth.welcome", NotificationChannel.Email),
            ["auth.password.changed"] = ("auth.password-changed", NotificationChannel.Email),
            ["auth.user.locked-out"] = ("auth.account-locked", NotificationChannel.Email),
            ["booking.created"] = ("booking.held", NotificationChannel.Email),
            ["booking.confirmed"] = ("booking.confirmed", NotificationChannel.Email),
            ["booking.cancelled"] = ("booking.cancelled", NotificationChannel.Email),
            ["payment.completed"] = ("payment.receipt", NotificationChannel.Email),
            ["payment.failed"] = ("payment.failed", NotificationChannel.Email),
        };

    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationEventConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public NotificationEventConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationEventConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.UpstreamBindings.Count == 0)
        {
            _logger.LogInformation("No RabbitMq:UpstreamBindings configured; NotificationEventConsumer will not start.");
            return Task.CompletedTask;
        }

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
            _connection = factory.CreateConnection("notification-service-consumer");
            _channel = _connection.CreateModel();

            const string queueName = "notification-service.upstream-events";
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

            foreach (var binding in _options.UpstreamBindings)
            {
                _channel.ExchangeDeclare(binding.Exchange, ExchangeType.Topic, durable: true);
                _channel.QueueBind(queueName, binding.Exchange, binding.RoutingKey);
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceivedAsync;
            _channel.BasicConsume(queueName, autoAck: false, consumer);

            _logger.LogInformation("NotificationEventConsumer subscribed to {BindingCount} upstream binding(s).", _options.UpstreamBindings.Count);
        }
        catch (Exception ex)
        {
            // Graceful degradation: RabbitMQ being down at startup must not
            // crash the whole API — REST/gRPC endpoints and the outbound
            // dispatch job are still fully functional without this consumer.
            _logger.LogError(ex,
                "Failed to start NotificationEventConsumer. Root cause: RabbitMQ unreachable at {HostName}:{Port} " +
                "or misconfigured. Possible solution: verify the RabbitMQ container/service is running and " +
                "RabbitMq:HostName/Port/UserName/Password are correct. Inbound event-driven notifications " +
                "(welcome emails, booking confirmations, etc.) will not fire until this is resolved; " +
                "REST/gRPC-triggered sends are unaffected.",
                _options.HostName, _options.Port);
        }

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var routingKey = args.RoutingKey;

        try
        {
            if (!RoutingKeyMap.TryGetValue(routingKey, out var mapping))
            {
                // Bound but unmapped routing key (a new upstream event type
                // was wired into UpstreamBindings before its RoutingKeyMap
                // entry was added) -- ack and drop rather than requeue-loop
                // forever on a message we will never know how to handle.
                _logger.LogWarning("Received event with unmapped routing key '{RoutingKey}'; acking and discarding.", routingKey);
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var recipient = ExtractRecipient(root, mapping.Channel, out var recipientId);
            if (recipient is null && !string.IsNullOrWhiteSpace(recipientId))
            {
                using var scope = _scopeFactory.CreateScope();
                var directoryClient = scope.ServiceProvider.GetRequiredService<IUserDirectoryClient>();
                var contact = await directoryClient.ResolveContactAsync(recipientId);
                recipient = mapping.Channel == NotificationChannel.Sms ? contact?.PhoneNumber : contact?.Email;
            }

            if (string.IsNullOrWhiteSpace(recipient))
            {
                _logger.LogWarning(
                    "Could not resolve a {Channel} recipient for event '{RoutingKey}' (payload had no inline contact " +
                    "field and directory resolution did not return one); skipping notification. Payload: {Payload}",
                    mapping.Channel, routingKey, json);
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            var variables = ToVariableBag(root);

            using var mediatorScope = _scopeFactory.CreateScope();
            var mediator = mediatorScope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new SendNotificationCommand(
                Recipient: recipient,
                Channel: mapping.Channel,
                TemplateKey: mapping.TemplateKey,
                TemplateVariables: variables,
                Subject: null,
                Body: null,
                DataPayload: null,
                RecipientId: recipientId,
                SourceReference: routingKey,
                Locale: null,
                Priority: NotificationPriority.Normal,
                ScheduledForUtc: null,
                MaxRetryCount: null,
                IsTransactional: true));

            if (!result.IsSuccess)
                _logger.LogWarning("SendNotification from event '{RoutingKey}' returned validation errors: {Errors}",
                    routingKey, string.Join("; ", result.Errors.Select(e => e.Message)));

            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process upstream event with routing key '{RoutingKey}'. Root cause: handler exception " +
                "(see stack trace). Possible solution: check SendNotificationValidator rules and the event payload " +
                "shape above. Message will be requeued once.",
                routingKey);

            // Requeue exactly once (redelivered=false -> nack+requeue;
            // redelivered=true -> ack+drop) so a transient failure gets one
            // retry without an infinite redelivery loop for a poison message.
            _channel!.BasicNack(args.DeliveryTag, multiple: false, requeue: !args.Redelivered);
        }
    }

    private static string? ExtractRecipient(JsonElement root, NotificationChannel channel, out string? recipientId)
    {
        recipientId = TryGetString(root, "UserId") ?? TryGetString(root, "CustomerId");

        return channel switch
        {
            NotificationChannel.Sms => TryGetString(root, "PhoneNumber") ?? TryGetString(root, "Phone"),
            _ => TryGetString(root, "Email")
        };
    }

    private static string? TryGetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>Flattens the event'\''s top-level JSON properties into a Scriban variable bag (camelCase keys) so templates can reference {{firstName}}, {{bookingId}}, etc. without this consumer needing per-event-type mapping code.</summary>
    private static Dictionary<string, object?> ToVariableBag(JsonElement root)
    {
        var variables = new Dictionary<string, object?>();
        if (root.ValueKind != JsonValueKind.Object) return variables;

        foreach (var property in root.EnumerateObject())
        {
            var key = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            variables[key] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True or JsonValueKind.False => property.Value.GetBoolean(),
                JsonValueKind.Array => property.Value.EnumerateArray().Select(e => e.ToString()).ToArray(),
                _ => property.Value.ToString()
            };
        }

        return variables;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
