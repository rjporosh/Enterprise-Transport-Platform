namespace AuthService.Domain.Exceptions;

public sealed class ModuleNotFoundException : DomainException
{
    public ModuleNotFoundException(Guid moduleId)
        : base($"Module '{moduleId}' not found.") { }
}
