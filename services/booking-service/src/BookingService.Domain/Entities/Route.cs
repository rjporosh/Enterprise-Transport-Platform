using BookingService.Domain.Common;

namespace BookingService.Domain.Entities;

/// <summary>
/// A reference entity owned by the Route Service in production; replicated
/// here (read-only, kept in sync via integration events) so Booking can
/// query trips without a synchronous cross-service call on the hot path.
/// </summary>
public class Route : Entity
{
    public string OriginCity { get; private set; } = default!;
    public string DestinationCity { get; private set; } = default!;
    public decimal DistanceKm { get; private set; }

    private Route() { } // EF Core

    public Route(Guid id, string originCity, string destinationCity, decimal distanceKm) : base(id)
    {
        OriginCity = originCity;
        DestinationCity = destinationCity;
        DistanceKm = distanceKm;
    }
}
