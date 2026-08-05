using System.Text.Json;
using BusService.Application.Common.Interfaces;
using BusService.Domain.Common;

namespace BusService.Infrastructure.Persistence.Outbox;

public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly BusDbContext _context;

    public OutboxEventPublisher(BusDbContext context) => _context = context;

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
