using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Contracts.Messaging;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Domain.Common;
using TicketingService.Infrastructure.Messaging;

namespace TicketingService.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTimeOffset OccurredOnUtc { get; set; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}

public sealed class OutboxEventPublisher(TicketingDbContext context) : IEventPublisher
{
    public Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = domainEvent.EventId,
            EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });
        return Task.CompletedTask;
    }
}

public sealed class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private const int Batch = 50;
    private const int MaxRetries = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Outbox loop failed; retrying after the interval."); }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessageBusPublisher>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOnUtc).Take(Batch).ToListAsync(ct);
        if (messages.Count == 0) return;

        foreach (var m in messages)
        {
            try
            {
                IntegrationEventRoutingKeys.TryResolve(m.EventType, "ticket", out var routingKey, out _);
                await publisher.PublishAsync(routingKey, m.Payload, ct);
                m.ProcessedOnUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                m.RetryCount++;
                m.Error = ex.Message;
                logger.LogWarning(ex, "Failed to publish outbox message {Id} (attempt {Retry})", m.Id, m.RetryCount);
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
