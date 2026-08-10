namespace AuthService.Domain.Exceptions;

public sealed class RoleNotFoundException : DomainException
{
    public RoleNotFoundException(Guid roleId)
        : base($"Role '{roleId}' not found.") { }
}
