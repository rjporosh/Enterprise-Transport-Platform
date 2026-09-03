using MediatR;

namespace TicketingService.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    protected Entity() { }
    protected Entity(Guid id) => Id = id;
}

public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> _domainEvents = new();

    protected AggregateRoot() { }
    protected AggregateRoot(Guid id) : base(id) { }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void Raise(DomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Optimistic concurrency token → Postgres <c>xmin</c>.</summary>
    public uint Version { get; set; }
}

public abstract record DomainEvent : INotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
