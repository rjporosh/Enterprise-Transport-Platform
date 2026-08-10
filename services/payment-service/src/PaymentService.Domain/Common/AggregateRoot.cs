using MediatR;

namespace PaymentService.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<INotification> _domainEvents = new();

    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(INotification domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
