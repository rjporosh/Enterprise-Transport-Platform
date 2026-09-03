using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Contracts.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TicketingService.Application.Features.Tickets;
using TicketingService.Infrastructure.Persistence;
using TicketingService.Infrastructure.Persistence.Inbox;

namespace TicketingService.Infrastructure.Messaging;

/// <summary>
/// Consumes <c>booking.confirmed</c> and issues a ticket. Inbox-deduplicated;
/// a redelivered event is acked without re-issuing (the handler is also
/// idempotent on BookingId). RabbitMQ down at startup → logged, the API stays up.
/// </summary>
public sealed class BookingConfirmedConsumer : BackgroundService
{
    private const string ConsumerName = "booking-confirmed";
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingConfirmedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public BookingConfirmedConsumer(IOptions<RabbitMqOptions> options, IServiceScopeFactory scopeFactory, ILogger<BookingConfirmedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _connection = new ConnectionFactory
            {
                HostName = _options.HostName, Port = _options.Port,
                UserName = _options.UserName, Password = _options.Password,
                DispatchConsumersAsync = true
            }.CreateConnection("ticketing-service:booking-confirmed");
            _channel = _connection.CreateModel();

            const string queue = "ticketing-service.booking-confirmed";
            _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
            _channel.ExchangeDeclare("booking.events", ExchangeType.Topic, durable: true);
            _channel.QueueBind(queue, "booking.events", EventTypes.BookingConfirmed);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnReceivedAsync;
            _channel.BasicConsume(queue, autoAck: false, consumer);
            _logger.LogInformation("BookingConfirmedConsumer subscribed to booking.events / {Key}.", EventTypes.BookingConfirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "BookingConfirmedConsumer failed to start. Root cause: RabbitMQ unreachable at {Host}:{Port}. " +
                "Possible solution: start RabbitMQ. Tickets will not be issued until the broker returns; the API stays up.",
                _options.HostName, _options.Port);
        }
        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var body = Encoding.UTF8.GetString(args.Body.ToArray());
        var messageId = Guid.TryParse(args.BasicProperties?.MessageId, out var mid) ? mid : DeterministicGuid(body);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();

            if (await db.InboxMessages.AsNoTracking().AnyAsync(m => m.Id == messageId && m.Consumer == ConsumerName))
            {
                _channel!.BasicAck(args.DeliveryTag, false);
                return;
            }

            using var doc = JsonDocument.Parse(body);
            var r = doc.RootElement;
            var command = new IssueTicketCommand(
                BookingId: G(r, "BookingId"),
                PaymentId: G(r, "PaymentId"),
                TripId: G(r, "TripId"),
                CustomerId: G(r, "CustomerId"),
                OperatorId: TryG(r, "OperatorId") ?? Guid.Empty,
                CustomerName: S(r, "CustomerName") ?? "Customer",
                CustomerEmail: S(r, "CustomerEmail") ?? string.Empty,
                CustomerPhone: S(r, "CustomerPhone"),
                OriginCity: S(r, "OriginCity") ?? "—",
                DestinationCity: S(r, "DestinationCity") ?? "—",
                DepartureUtc: D(r, "DepartureUtc"),
                ArrivalUtc: D(r, "ArrivalUtc"),
                BusPlateNumber: S(r, "BusPlateNumber") ?? "—",
                BusType: S(r, "BusType") ?? "—",
                TotalAmount: r.TryGetProperty("TotalAmount", out var amt) ? amt.GetDecimal() : 0m,
                Currency: S(r, "Currency") ?? "BDT",
                Seats: Zip(r));

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(command, CancellationToken.None);

            db.InboxMessages.Add(new InboxMessage
            {
                Id = messageId, Consumer = ConsumerName, RoutingKey = args.RoutingKey,
                ReceivedAtUtc = DateTimeOffset.UtcNow, ProcessedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
            _channel!.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BookingConfirmedConsumer failed on '{RoutingKey}'. Payload: {Payload}", args.RoutingKey, body);
            _channel!.BasicNack(args.DeliveryTag, false, requeue: !args.Redelivered);
        }
    }

    private static IReadOnlyCollection<PassengerSeat> Zip(JsonElement r)
    {
        var seats = r.TryGetProperty("SeatNumbers", out var s) && s.ValueKind == JsonValueKind.Array
            ? s.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>();
        var names = r.TryGetProperty("PassengerNames", out var p) && p.ValueKind == JsonValueKind.Array
            ? p.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>();
        return seats.Select((seat, i) => new PassengerSeat(seat, i < names.Count ? names[i] : "Passenger")).ToList();
    }

    private static Guid G(JsonElement r, string n) => Guid.TryParse(S(r, n), out var g) ? g : Guid.Empty;
    private static Guid? TryG(JsonElement r, string n) => Guid.TryParse(S(r, n), out var g) ? g : null;
    private static string? S(JsonElement r, string n) => r.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static DateTimeOffset D(JsonElement r, string n) => r.TryGetProperty(n, out var v) && v.TryGetDateTimeOffset(out var d) ? d : DateTimeOffset.UtcNow;

    private static Guid DeterministicGuid(string s) => new(System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(s)));

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
