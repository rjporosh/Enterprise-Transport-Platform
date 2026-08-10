namespace AuthService.Domain.Exceptions;

public sealed class AccountLockedException : DomainException
{
    public DateTimeOffset LockedUntil { get; }

    public AccountLockedException(DateTimeOffset lockedUntilUtc)
        : base($"This account is temporarily locked until {lockedUntilUtc:u} due to too many failed sign-in attempts.")
    {
        LockedUntil = lockedUntilUtc;
    }
}
