using NotificationService.Domain.Common;

namespace NotificationService.Application.Common.Interfaces;

/// <summary>
/// Writes a domain event into the transactional outbox table (same DB
/// transaction as the aggregate change), never directly onto the message
/// bus — identical contract/guarantee to every other service in this
/// solution (see docs/events/outbox-pattern.md).
/// </summary>
public interface IEventPublisher
{
    Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
