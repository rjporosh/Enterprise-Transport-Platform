using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Infrastructure.Messaging;
using Platform.Contracts.Messaging;

namespace PaymentService.Infrastructure.Persistence.Outbox;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBusPublisher _messageBusPublisher;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 50;
    private readonly int _maxRetries = 5;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        IMessageBusPublisher messageBusPublisher,
        ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _messageBusPublisher = messageBusPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started. Polling interval: {Interval}s", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor encountered an error");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        var outboxMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < _maxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (outboxMessages.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} outbox messages", outboxMessages.Count);

        foreach (var message in outboxMessages)
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

                await _messageBusPublisher.PublishAsync(routingKey, message.Payload, cancellationToken);

                message.ProcessedOnUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to publish outbox message {OutboxId}. Retry {RetryCount}/{MaxRetries}",
                    message.Id,
                    message.RetryCount + 1,
                    _maxRetries);

                message.RetryCount++;
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Service prefix for the deterministic fallback in
    /// <see cref="IntegrationEventRoutingKeys"/>. Known payment events resolve
    /// from the explicit <c>Platform.Contracts.EventTypeRegistry</c>
    /// (<c>payment.succeeded</c>/<c>payment.failed</c>/…). The old
    /// <c>DeriveRoutingKey</c> split the stored AssemblyQualifiedName on '.',
    /// producing keys like <c>payment.0, culture=neutral…</c> (P0-4). It is gone.
    /// </summary>
    private const string ServicePrefix = "payment";
}
