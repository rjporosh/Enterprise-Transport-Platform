namespace BookingService.Domain.Common;

/// <summary>
/// An Entity that is the transactional consistency boundary ("aggregate root")
/// for a cluster of related objects. Only aggregate roots raise domain events
/// and are loaded/saved directly by repositories/DbContext.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> _domainEvents = new();

    protected AggregateRoot() { }
    protected AggregateRoot(Guid id) : base(id) { }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Optimistic concurrency token mapped to Postgres' `xmin` system column.
    /// Prevents two customers from double-booking the same seat under
    /// concurrent load without taking an explicit row lock.
    /// </summary>
    public uint Version { get; set; }
}
