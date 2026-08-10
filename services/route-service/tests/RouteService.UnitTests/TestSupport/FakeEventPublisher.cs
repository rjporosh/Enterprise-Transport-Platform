using RouteService.Application.Common.Interfaces;

namespace RouteService.UnitTests.TestSupport;

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<RouteService.Domain.Common.DomainEvent> PublishedEvents { get; } = new();

    public Task EnqueueAsync(RouteService.Domain.Common.DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
