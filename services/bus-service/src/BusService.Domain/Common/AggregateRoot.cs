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
    /// Optimistic concurrency token. Bus Service is DB-provider-switchable
    /// (Postgres/SqlServer/MySQL — see Infrastructure/DependencyInjection.cs),
    /// so a native concurrency column (Postgres `xmin`, SQL Server
    /// `rowversion`) isn't portable across all of them. Left unmapped
    /// (`Ignore`d in BusConfiguration) rather than faked with a
    /// provider-specific column — same trade-off Auth Service's User
    /// aggregate makes, for the same reason.
    /// </summary>
    public uint Version { get; set; }
}
