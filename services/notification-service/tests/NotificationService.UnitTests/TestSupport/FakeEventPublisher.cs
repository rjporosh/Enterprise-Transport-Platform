using NotificationService.Application.Common.Interfaces;
using NotificationService.Domain.Common;

namespace NotificationService.UnitTests.TestSupport;

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<DomainEvent> PublishedEvents { get; } = new();

    public Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
