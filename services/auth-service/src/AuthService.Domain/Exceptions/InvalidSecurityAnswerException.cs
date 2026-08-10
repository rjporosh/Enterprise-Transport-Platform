namespace AuthService.Domain.Exceptions;

public sealed class InvalidSecurityAnswerException : DomainException
{
    public InvalidSecurityAnswerException(string message)
        : base(message) { }
}
