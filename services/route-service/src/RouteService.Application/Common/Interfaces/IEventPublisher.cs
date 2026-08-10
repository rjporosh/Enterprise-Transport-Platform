namespace RouteService.Application.Common.Interfaces;

public interface IEventPublisher
{
    Task EnqueueAsync(RouteService.Domain.Common.DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
