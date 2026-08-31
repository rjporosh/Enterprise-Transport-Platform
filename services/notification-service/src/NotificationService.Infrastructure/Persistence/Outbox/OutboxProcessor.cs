using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Infrastructure.Messaging;
using Platform.Contracts.Messaging;

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
                IntegrationEventRoutingKeys.TryResolve(message.EventType, ServicePrefix, out var routingKey, out var fromRegistry);
                if (!fromRegistry)
                {
                    _logger.LogWarning(
                        "Outbox event type {EventType} is not in Platform.Contracts EventTypeRegistry; " +
                        "published with the deterministic fallback key {RoutingKey}. Add it to the registry.",
                        message.EventType, routingKey);
                }

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

    /// <summary>
    /// Service prefix for the deterministic fallback in
    /// <see cref="IntegrationEventRoutingKeys"/>. Known notification events
    /// resolve from the explicit <c>Platform.Contracts.EventTypeRegistry</c>.
    /// The old munging produced <c>notification.notification.sent</c> (P0-4) and is gone.
    /// </summary>
    private const string ServicePrefix = "notification";
}
