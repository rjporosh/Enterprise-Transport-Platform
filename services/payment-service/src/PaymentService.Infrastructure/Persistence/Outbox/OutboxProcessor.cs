using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Infrastructure.Messaging;

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
                var routingKey = DeriveRoutingKey(message.EventType);
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

    private static string DeriveRoutingKey(string eventType)
    {
        var typeName = eventType.Split('.').Last();
        if (typeName.EndsWith("DomainEvent"))
            typeName = typeName[..^"DomainEvent".Length];

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < typeName.Length; i++)
        {
            if (char.IsUpper(typeName[i]) && i > 0)
                sb.Append('.');
            sb.Append(char.ToLowerInvariant(typeName[i]));
        }

        return "payment." + sb.ToString();
    }
}
