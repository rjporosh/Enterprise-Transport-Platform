using BookingService.Domain.Common;

namespace BookingService.Application.Common.Interfaces;

/// <summary>
/// Writes a domain event into the transactional outbox table (same DB
/// transaction as the aggregate change), NOT directly onto the message bus.
/// A background OutboxProcessor in Infrastructure later relays outbox rows
/// to RabbitMQ, giving us at-least-once delivery even if the process
/// crashes between commit and publish.
/// </summary>
public interface IEventPublisher
{
    Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
