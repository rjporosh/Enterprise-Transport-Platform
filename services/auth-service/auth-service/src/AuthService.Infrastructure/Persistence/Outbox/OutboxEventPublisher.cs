using System.Text.Json;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Common;

namespace AuthService.Infrastructure.Persistence.Outbox;

/// <summary>
/// Adds the serialized event to the change tracker; does NOT call
/// SaveChangesAsync itself — the calling command handler commits it in the
/// same transaction as the aggregate change it belongs to.
/// </summary>
public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly AuthDbContext _context;

    public OutboxEventPublisher(AuthDbContext context) => _context = context;

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
