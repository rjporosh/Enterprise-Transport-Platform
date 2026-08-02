namespace AuthService.Application.Common.Interfaces;

/// <summary>Abstraction over "now" so time-dependent logic (lockout expiry, token TTLs) is testable.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
