using BusService.Application.Common.Interfaces;
using BusService.Domain.Common;

namespace BusService.UnitTests.TestSupport;

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<DomainEvent> PublishedEvents { get; } = new();

    public Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
