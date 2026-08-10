namespace AuthService.Domain.Entities;

public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;
    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = default!;
    public DateTimeOffset AssignedAtUtc { get; private set; }

    private RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId, DateTimeOffset assignedAtUtc)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        AssignedAtUtc = assignedAtUtc;
    }
}
