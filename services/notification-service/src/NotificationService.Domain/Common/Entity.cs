namespace NotificationService.Domain.Common;

/// <summary>
/// Base type for all entities identified by a stable Guid identity rather
/// than by their attribute values. Copied verbatim from the platform-wide
/// convention established in BookingService.Domain / AuthService.Domain —
/// each service owns its own copy since shared/shared-kernel is not yet
/// populated (see .ai/AI_RULES.md: never introduce circular dependencies;
/// a cross-service package dependency for a handful of base classes is not
/// worth the coupling it would create between independently deployable
/// services).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity() { }

    protected Entity(Guid id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();

    public static bool operator ==(Entity? a, Entity? b) =>
        a is null && b is null || (a is not null && a.Equals(b));

    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}
