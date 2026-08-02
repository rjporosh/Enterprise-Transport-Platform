namespace AuthService.Domain.Exceptions;

public sealed class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string email)
        : base($"An account with email '{email}' already exists.") { }
}
