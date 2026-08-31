using AuthService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Contracts.Messaging;

namespace AuthService.Infrastructure.Persistence.Outbox;

/// <summary>
/// Polls the outbox table and relays unprocessed rows to RabbitMQ. Same
/// polling-based design as BookingService's OutboxProcessor — see that
/// service's docs for the LISTEN/NOTIFY or Debezium upgrade path.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;
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
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
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
                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId} (attempt {RetryCount})", message.Id, message.RetryCount);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Service prefix for the deterministic fallback in
    /// <see cref="IntegrationEventRoutingKeys"/>. Known auth events resolve from
    /// the explicit <c>Platform.Contracts.EventTypeRegistry</c> (pinned to the
    /// exact keys this service already emitted, so this change is a no-op for
    /// auth — no regression). The old name-munging that this replaces is gone.
    /// </summary>
    private const string ServicePrefix = "auth";
}
