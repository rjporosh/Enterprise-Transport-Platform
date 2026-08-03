namespace AuthService.Domain.Common;

/// <summary>
/// An Entity that is the transactional consistency boundary ("aggregate root")
/// for a cluster of related objects. Only aggregate roots raise domain events
/// and are loaded/saved directly by the DbContext.
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
    /// Optimistic concurrency token. Mapped to a provider-appropriate
    /// row-version column by Infrastructure (Postgres xmin, SQL Server
    /// rowversion, or an explicit int for providers with neither — see
    /// docs/architecture/auth-service-architecture.md, "Database portability").
    /// </summary>
    public uint Version { get; set; }
}
