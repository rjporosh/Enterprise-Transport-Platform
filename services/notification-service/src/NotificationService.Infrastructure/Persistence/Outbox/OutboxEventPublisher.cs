using System.Text.Json;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Domain.Common;

namespace NotificationService.Infrastructure.Persistence.Outbox;

/// <summary>Stages an outbox row on the change tracker; does NOT call SaveChangesAsync -- the handler commits it in the same transaction as the aggregate change that raised the event (outbox-pattern atomicity guarantee).</summary>
public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly NotificationDbContext _context;

    public OutboxEventPublisher(NotificationDbContext context) => _context = context;

    public Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = domainEvent.EventId,
            EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        _context.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }
}
