namespace AuthService.Domain.Entities;

public sealed class ModulePermission
{
    public Guid ModuleId { get; private set; }
    public Module Module { get; private set; } = default!;
    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = default!;
    public DateTimeOffset AssignedAtUtc { get; private set; }

    private ModulePermission() { }

    public ModulePermission(Guid moduleId, Guid permissionId, DateTimeOffset assignedAtUtc)
    {
        ModuleId = moduleId;
        PermissionId = permissionId;
        AssignedAtUtc = assignedAtUtc;
    }
}
