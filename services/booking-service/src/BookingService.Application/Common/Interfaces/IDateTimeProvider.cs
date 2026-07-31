namespace BookingService.Application.Common.Interfaces;

/// <summary>Abstraction over "now" so time-dependent logic (hold expiry, etc.) is testable.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
