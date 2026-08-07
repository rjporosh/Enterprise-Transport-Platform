namespace NotificationService.Domain.Common;

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
    /// Optimistic concurrency token mapped to Postgres xmin (or a rowversion
    /// column on SqlServer/MySql — see NotificationDbContext provider-specific
    /// mapping). Prevents e.g. two concurrent retry jobs from double-sending
    /// the same notification.
    /// </summary>
    public uint Version { get; set; }
}
