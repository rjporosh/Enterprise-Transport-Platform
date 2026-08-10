using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Infrastructure.Messaging;

namespace NotificationService.Infrastructure.Persistence.Outbox;

/// <summary>Polls the outbox table and relays unprocessed rows to RabbitMQ -- same polling design (and same trade-off note re: Postgres LISTEN/NOTIFY or Debezium CDC as a lower-latency drop-in) as BookingService/AuthService's OutboxProcessor.</summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 100;
    private const int MaxRetries = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing loop failed unexpectedly; will retry after the poll interval.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessageBusPublisher>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                var routingKey = ToRoutingKey(message.EventType);
                await publisher.PublishAsync(routingKey, message.Payload, cancellationToken);
                message.ProcessedOnUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId} (attempt {RetryCount})",
                    message.Id, message.RetryCount);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>"NotificationService.Domain.Events.NotificationSentDomainEvent, ..." -&gt; "notification.sent"</summary>
    private static string ToRoutingKey(string assemblyQualifiedEventType)
    {
        var shortName = assemblyQualifiedEventType.Split(',')[0].Split('.').Last();
        var withoutSuffix = shortName.Replace("DomainEvent", string.Empty);
        return "notification." + string.Concat(withoutSuffix.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "." + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}
