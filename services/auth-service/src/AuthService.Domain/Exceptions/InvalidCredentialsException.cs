namespace AuthService.Domain.Exceptions;

/// <summary>
/// Deliberately generic message — never reveal whether the email or the
/// password was the part that was wrong, that is a user-enumeration leak.
/// </summary>
public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() : base("The email or password is incorrect.") { }
}
