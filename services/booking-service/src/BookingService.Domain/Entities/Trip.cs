using BookingService.Domain.Common;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;

namespace BookingService.Domain.Entities;

/// <summary>
/// Aggregate root representing one scheduled departure of a Bus along a Route.
/// Owns seat inventory (<see cref="TripSeat"/>) and is the consistency
/// boundary that prevents two bookings from claiming the same seat: every
/// seat mutation happens through this aggregate and is protected by EF Core
/// optimistic concurrency (<see cref="AggregateRoot.Version"/>, mapped to
/// Postgres `xmin`).
/// </summary>
public class Trip : AggregateRoot
{
    private readonly List<TripSeat> _seats = new();

    public Guid RouteId { get; private set; }
    public Guid BusId { get; private set; }
    public DateTimeOffset DepartureUtc { get; private set; }
    public DateTimeOffset ArrivalUtc { get; private set; }
    public Money BasePrice { get; private set; }
    public TripStatus Status { get; private set; } = TripStatus.Scheduled;

    public IReadOnlyCollection<TripSeat> Seats => _seats.AsReadOnly();

    private Trip() { } // EF Core

    public Trip(
        Guid id,
        Guid routeId,
        Guid busId,
        DateTimeOffset departureUtc,
        DateTimeOffset arrivalUtc,
        Money basePrice,
        IEnumerable<(string SeatNumber, string Deck)> seatLayout) : base(id)
    {
        if (arrivalUtc <= departureUtc)
            throw new ArgumentException("Arrival must be after departure.", nameof(arrivalUtc));

        RouteId = routeId;
        BusId = busId;
        DepartureUtc = departureUtc;
        ArrivalUtc = arrivalUtc;
        BasePrice = basePrice;

        foreach (var (seatNumber, deck) in seatLayout)
            _seats.Add(new TripSeat(Guid.NewGuid(), Id, seatNumber, deck));
    }

    public int AvailableSeatCount => _seats.Count(s => s.Status == Enums.SeatStatus.Available);

    /// <summary>
    /// Attempts to move the requested seats from Available -&gt; Held.
    /// Throws <see cref="SeatUnavailableException"/> on the first seat that
    /// isn't free, so the whole hold either succeeds atomically or not at all.
    /// </summary>
    public void HoldSeats(IReadOnlyCollection<string> seatNumbers)
    {
        if (Status != TripStatus.Scheduled)
            throw new InvalidBookingStateException($"Trip {Id} is not open for booking (status: {Status}).");

        var seatsByNumber = _seats.ToDictionary(s => s.SeatNumber);
        foreach (var seatNumber in seatNumbers)
        {
            if (!seatsByNumber.TryGetValue(seatNumber, out var seat) || seat.Status != Enums.SeatStatus.Available)
                throw new SeatUnavailableException(seatNumber, Id);
        }

        foreach (var seatNumber in seatNumbers)
            seatsByNumber[seatNumber].Hold();
    }

    public void ConfirmSeats(IReadOnlyCollection<string> seatNumbers)
    {
        var seatsByNumber = _seats.ToDictionary(s => s.SeatNumber);
        foreach (var seatNumber in seatNumbers)
            seatsByNumber[seatNumber].Confirm();
    }

    public void ReleaseSeats(IReadOnlyCollection<string> seatNumbers)
    {
        var seatsByNumber = _seats.ToDictionary(s => s.SeatNumber);
        foreach (var seatNumber in seatNumbers)
        {
            if (seatsByNumber.TryGetValue(seatNumber, out var seat))
                seat.Release();
        }
    }
}
