namespace BusService.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> _domainEvents = new();

    protected AggregateRoot() { }
    protected AggregateRoot(Guid id) : base(id) { }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Optimistic concurrency token, mapped to Postgres' native `xmin` system
    /// column on Bus (see BusConfiguration) — genuinely used here, unlike
    /// Auth Service's User aggregate, because bus status changes (e.g.
    /// "assign to a trip" vs "mark under maintenance") are exactly the kind
    /// of concurrent-write race this exists to catch.
    /// </summary>
    public uint Version { get; set; }
}
