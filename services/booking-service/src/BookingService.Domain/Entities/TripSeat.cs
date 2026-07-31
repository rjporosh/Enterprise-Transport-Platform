using BookingService.Domain.Common;
using BookingService.Domain.Enums;

namespace BookingService.Domain.Entities;

/// <summary>
/// A single physical seat's availability for one specific Trip. Child entity
/// of the Trip aggregate — always mutated through Trip so the aggregate's
/// invariants (no double-allocation) hold.
/// </summary>
public class TripSeat : Entity
{
    public Guid TripId { get; private set; }
    public string SeatNumber { get; private set; } = default!;
    public string Deck { get; private set; } = "Lower"; // "Lower" | "Upper"
    public SeatStatus Status { get; private set; } = SeatStatus.Available;

    private TripSeat() { } // EF Core

    public TripSeat(Guid id, Guid tripId, string seatNumber, string deck) : base(id)
    {
        TripId = tripId;
        SeatNumber = seatNumber;
        Deck = deck;
    }

    internal void Hold()
    {
        if (Status != SeatStatus.Available)
            throw new InvalidOperationException($"Seat {SeatNumber} cannot be held from state {Status}.");
        Status = SeatStatus.Held;
    }

    internal void Confirm()
    {
        if (Status != SeatStatus.Held)
            throw new InvalidOperationException($"Seat {SeatNumber} cannot be confirmed from state {Status}.");
        Status = SeatStatus.Booked;
    }

    internal void Release()
    {
        if (Status is SeatStatus.OutOfService) return;
        Status = SeatStatus.Available;
    }
}
