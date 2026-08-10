using MediatR;

namespace PaymentService.Domain.Common;

public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredOnUtc) : INotification
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTimeOffset.UtcNow) { }
}
