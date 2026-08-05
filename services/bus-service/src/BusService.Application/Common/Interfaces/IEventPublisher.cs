using BusService.Domain.Common;

namespace BusService.Application.Common.Interfaces;

/// <summary>Writes a domain event into the transactional outbox table (same DB transaction as the aggregate change), not directly onto the bus. Same pattern as Auth/Booking Service.</summary>
public interface IEventPublisher
{
    Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
