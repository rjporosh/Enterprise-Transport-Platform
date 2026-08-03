namespace AuthService.Domain.Exceptions;

public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId) : base($"User '{userId}' was not found.") { }
    public UserNotFoundException(string email) : base($"User '{email}' was not found.") { }
}
