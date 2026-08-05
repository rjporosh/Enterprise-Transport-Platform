namespace BusService.Domain.Enums;

/// <summary>
/// Mirrors the free-text values Booking Service's local Bus replica already
/// stores as a plain string (see that service's Entities/Bus.cs) — this enum
/// is the strongly-typed source of truth; ToString() on these values is
/// exactly what gets published in BusRegisteredDomainEvent and is expected
/// to round-trip through Booking Service's sync consumer unchanged.
/// </summary>
public enum BusType
{
    AcSeater = 0,
    AcSleeper = 1,
    NonAcSeater = 2,
    NonAcSleeper = 3
}
