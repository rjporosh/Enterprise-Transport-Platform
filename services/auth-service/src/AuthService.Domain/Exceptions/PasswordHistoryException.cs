namespace AuthService.Domain.Exceptions;

public sealed class PasswordHistoryException : DomainException
{
    public PasswordHistoryException(string message) : base(message) { }
}
