namespace AuthService.Domain.Exceptions;

public sealed class InvalidResetTokenException : DomainException
{
    public InvalidResetTokenException()
        : base("Invalid or expired password reset token.") { }
}
