using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Common;

namespace PaymentService.Infrastructure.Persistence.Outbox;

public class OutboxEventPublisher : IEventPublisher
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<OutboxEventPublisher> _logger;

    public OutboxEventPublisher(PaymentDbContext context, ILogger<OutboxEventPublisher> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task EnqueueAsync(INotification domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType().AssemblyQualifiedName!;
        var payload = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            OccurredOnUtc = domainEvent is DomainEvent de ? de.OccurredOnUtc : DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        _context.OutboxMessages.Add(outboxMessage);

        _logger.LogDebug(
            "Enqueued outbox event {EventType} with ID {OutboxId}",
            eventType,
            outboxMessage.Id);

        return Task.CompletedTask;
    }
}
