namespace AuthService.Domain.Entities;

/// <summary>Join entity for the User &lt;-&gt; Role many-to-many relationship.</summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    public DateTimeOffset AssignedAtUtc { get; private set; }

    private UserRole() { }

    public UserRole(Guid userId, Guid roleId, DateTimeOffset assignedAtUtc)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc;
    }
}
