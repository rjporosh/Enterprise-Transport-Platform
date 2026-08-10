namespace AuthService.Domain.Exceptions;

public sealed class PermissionNotFoundException : DomainException
{
    public PermissionNotFoundException(Guid permissionId)
        : base($"Permission '{permissionId}' not found.") { }
}
