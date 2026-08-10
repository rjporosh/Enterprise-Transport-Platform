using Microsoft.Extensions.Options;
using RouteService.Application.Common.Interfaces;
using RouteService.Infrastructure.Messaging;
using RouteService.Infrastructure.Persistence;

namespace RouteService.Infrastructure.Persistence.Outbox;

public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly RouteDbContext _context;
    private readonly IMessageBusPublisher _messageBusPublisher;

    public OutboxEventPublisher(RouteDbContext context, IMessageBusPublisher messageBusPublisher)
    {
        _context = context;
        _messageBusPublisher = messageBusPublisher;
    }

    public async Task EnqueueAsync(RouteService.Domain.Common.DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = domainEvent.GetType().AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
