using BookingService.Domain.Common;

namespace BookingService.Domain.Entities;

/// <summary>
/// A reference entity owned by the Bus Service in production; replicated
/// (read-only) into Booking's local store for the same reason as Route.
/// </summary>
public class Bus : Entity
{
    public Guid OperatorId { get; private set; }
    public string PlateNumber { get; private set; } = default!;
    public string BusType { get; private set; } = default!; // e.g. "AC Sleeper", "Non-AC Seater"
    public int TotalSeats { get; private set; }

    private Bus() { } // EF Core

    public Bus(Guid id, Guid operatorId, string plateNumber, string busType, int totalSeats) : base(id)
    {
        OperatorId = operatorId;
        PlateNumber = plateNumber;
        BusType = busType;
        TotalSeats = totalSeats;
    }
}
