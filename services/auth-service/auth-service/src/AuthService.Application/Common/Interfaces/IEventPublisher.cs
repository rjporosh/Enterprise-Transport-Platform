using AuthService.Domain.Common;

namespace AuthService.Application.Common.Interfaces;

/// <summary>
/// Writes a domain event into the transactional outbox table (same DB
/// transaction as the aggregate change), NOT directly onto the message bus.
/// See BookingService's equivalent for the full rationale — identical pattern.
/// </summary>
public interface IEventPublisher
{
    Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
