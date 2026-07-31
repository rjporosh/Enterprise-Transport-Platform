using BookingService.Domain.Common;
using BookingService.Domain.Enums;
using BookingService.Domain.Events;
using BookingService.Domain.Exceptions;

namespace BookingService.Domain.Entities;

/// <summary>
/// Aggregate root for a customer's reservation on a Trip. Independent of the
/// Trip aggregate on purpose — a Booking references a TripId rather than the
/// Trip object, so the two aggregates can be persisted in separate
/// transactions/rows and scale independently. Seat-availability consistency
/// between the two is enforced in <c>CreateBookingHandler</c> by holding
/// seats on Trip first, inside the same DB transaction as creating the Booking.
/// </summary>
public class Booking : AggregateRoot
{
    private readonly List<BookingSeat> _seats = new();

    public Guid TripId { get; private set; }
    public Guid CustomerId { get; private set; }
    public BookingStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>Holds expire if payment isn't completed by this time, freeing the seats.</summary>
    public DateTimeOffset HoldExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<BookingSeat> Seats => _seats.AsReadOnly();

    private Booking() { } // EF Core

    private Booking(Guid id, Guid tripId, Guid customerId, Money totalAmount, DateTimeOffset now) : base(id)
    {
        TripId = tripId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Status = BookingStatus.PendingPayment;
        CreatedAtUtc = now;
        HoldExpiresAtUtc = now.AddMinutes(10);
    }

    public static Booking Create(
        Guid tripId,
        Guid customerId,
        Money pricePerSeat,
        IReadOnlyCollection<(string SeatNumber, string PassengerFullName, int PassengerAge, string PassengerGender)> passengers,
        DateTimeOffset now)
    {
        if (passengers.Count == 0)
            throw new InvalidBookingStateException("A booking must include at least one passenger.");

        var total = pricePerSeat * passengers.Count;
        var booking = new Booking(Guid.NewGuid(), tripId, customerId, total, now);

        foreach (var p in passengers)
            booking._seats.Add(new BookingSeat(Guid.NewGuid(), booking.Id, p.SeatNumber, p.PassengerFullName, p.PassengerAge, p.PassengerGender));

        booking.Raise(new BookingCreatedDomainEvent(
            booking.Id,
            booking.TripId,
            booking.CustomerId,
            booking.TotalAmount.Amount,
            booking.TotalAmount.Currency,
            booking._seats.Select(s => s.SeatNumber).ToList()));

        return booking;
    }

    public IReadOnlyCollection<string> SeatNumbers => _seats.Select(s => s.SeatNumber).ToList();

    public void Confirm(DateTimeOffset now)
    {
        if (Status != BookingStatus.PendingPayment)
            throw new InvalidBookingStateException($"Booking {Id} cannot be confirmed from state {Status}.");

        Status = BookingStatus.Confirmed;
        ConfirmedAtUtc = now;
        Raise(new BookingConfirmedDomainEvent(Id, TripId, CustomerId));
    }

    public void Cancel(string reason, DateTimeOffset now)
    {
        if (Status is BookingStatus.Cancelled or BookingStatus.Refunded)
            throw new InvalidBookingStateException($"Booking {Id} is already {Status}.");

        Status = BookingStatus.Cancelled;
        CancelledAtUtc = now;
        CancellationReason = reason;
        Raise(new BookingCancelledDomainEvent(Id, TripId, reason));
    }

    public bool IsHoldExpired(DateTimeOffset now) => Status == BookingStatus.PendingPayment && now > HoldExpiresAtUtc;
}
