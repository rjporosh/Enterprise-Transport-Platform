using System.Text.Json;
using BookingService.Application.Common.Interfaces;
using BookingService.Domain.Common;

namespace BookingService.Infrastructure.Persistence.Outbox;

/// <summary>
/// Serializes the domain event and adds it to the DbContext change tracker.
/// Deliberately does NOT call SaveChangesAsync — the caller (a command
/// handler) commits it together with the aggregate change it belongs to,
/// which is what gives us the outbox pattern's atomicity guarantee.
/// </summary>
public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly BookingDbContext _context;

    public OutboxEventPublisher(BookingDbContext context) => _context = context;

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
