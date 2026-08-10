using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Persistence.Outbox;
using Quartz;

namespace PaymentService.Infrastructure.Jobs;

public class FailedWebhookRetryJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FailedWebhookRetryJob> _logger;
    private readonly int _maxRetries = 5;

    public FailedWebhookRetryJob(IServiceProvider serviceProvider, ILogger<FailedWebhookRetryJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("FailedWebhookRetryJob started");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var messageBusPublisher = scope.ServiceProvider.GetRequiredService<IMessageBusPublisher>();

        var failedMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < _maxRetries && m.Error != null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(50)
            .ToListAsync(context.CancellationToken);

        if (failedMessages.Count == 0)
        {
            _logger.LogInformation("No failed outbox messages found for retry");
            return;
        }

        _logger.LogInformation("Retrying {Count} failed outbox messages", failedMessages.Count);

        foreach (var message in failedMessages)
        {
            try
            {
                var routingKey = DeriveRoutingKey(message.EventType);
                await messageBusPublisher.PublishAsync(routingKey, message.Payload, context.CancellationToken);

                message.ProcessedOnUtc = DateTimeOffset.UtcNow;
                message.Error = null;
                _logger.LogInformation("Successfully retried outbox message {OutboxId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Retry failed for outbox message {OutboxId}. Retry {RetryCount}/{MaxRetries}", message.Id, message.RetryCount + 1, _maxRetries);
                message.RetryCount++;
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("FailedWebhookRetryJob completed");
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
