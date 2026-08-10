using MediatR;
using PaymentService.Application.Common.Interfaces;

namespace PaymentService.UnitTests.TestSupport;

public class FakeEventPublisher : IEventPublisher
{
    public List<object> PublishedEvents { get; } = new();

    public Task EnqueueAsync(INotification domainEvent, CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
