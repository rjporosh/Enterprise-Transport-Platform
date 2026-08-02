namespace AuthService.Domain.Exceptions;

public sealed class AccountNotActiveException : DomainException
{
    public AccountNotActiveException() : base("This account is not active. Verify your email or contact support.") { }
}
