using BookingService.Domain.Common;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.Events;
using BookingService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BookingService.UnitTests.Bookings;

public class BookingTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly (string SeatNumber, string PassengerFullName, int PassengerAge, string PassengerGender)[] OnePassenger =
    {
        ("A1", "Porosh Ahmed", 30, "Male")
    };

    private static readonly TripJourneyInfo Journey = new(
        "Dhaka", "Chattogram", Now.AddHours(2), Now.AddHours(8), "DHK-METRO-11-2345", "AC Sleeper", Guid.NewGuid());

    private static Booking Create(
        (string, string, int, string)[]? passengers = null,
        Money? price = null) =>
        Booking.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "customer@example.com", "Porosh Ahmed", "+8801700000000",
            price ?? new Money(500m, "BDT"),
            passengers ?? OnePassenger,
            Now);

    [Fact]
    public void Create_SetsStatusToPendingPayment_AndComputesTotalFromSeatCount()
    {
        var booking = Create(new[]
        {
            ("A1", "Porosh Ahmed", 30, "Male"),
            ("A2", "Jane Doe", 27, "Female")
        });

        booking.Status.Should().Be(BookingStatus.PendingPayment);
        booking.TotalAmount.Amount.Should().Be(1000m);
        booking.Seats.Should().HaveCount(2);
        booking.HoldExpiresAtUtc.Should().Be(Now.AddMinutes(10));
        booking.CustomerEmail.Should().Be("customer@example.com");
    }

    [Fact]
    public void Create_RaisesBookingCreatedDomainEvent()
    {
        Create().DomainEvents.Should().ContainSingle(e => e is BookingCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithNoPassengers_ThrowsInvalidBookingStateException()
    {
        var act = () => Create(Array.Empty<(string, string, int, string)>());
        act.Should().Throw<InvalidBookingStateException>();
    }

    [Fact]
    public void Confirm_FromPendingPayment_SetsConfirmedAndRaisesEvent()
    {
        var booking = Create();
        booking.ClearDomainEvents();

        booking.Confirm(Now.AddMinutes(2), Journey, Guid.NewGuid());

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedAtUtc.Should().Be(Now.AddMinutes(2));
        booking.DomainEvents.Should().ContainSingle(e => e is BookingConfirmedDomainEvent);
    }

    [Fact]
    public void Confirm_WhenAlreadyCancelled_ThrowsInvalidBookingStateException()
    {
        var booking = Create();
        booking.Cancel("Changed my mind", Now.AddMinutes(1));

        var act = () => booking.Confirm(Now.AddMinutes(2), Journey, Guid.NewGuid());

        act.Should().Throw<InvalidBookingStateException>();
    }

    [Fact]
    public void Cancel_ReleasesReasonAndRaisesEvent()
    {
        var booking = Create();
        booking.ClearDomainEvents();

        booking.Cancel("Customer requested refund", Now.AddMinutes(3));

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("Customer requested refund");
        booking.DomainEvents.Should().ContainSingle(e => e is BookingCancelledDomainEvent);
    }

    [Fact]
    public void Expire_FromPendingPayment_SetsExpiredAndRaisesCancelledEvent()
    {
        var booking = Create();
        booking.ClearDomainEvents();

        booking.Expire(Now.AddMinutes(11));

        booking.Status.Should().Be(BookingStatus.Expired);
        booking.DomainEvents.Should().ContainSingle(e => e is BookingCancelledDomainEvent);
    }

    [Fact]
    public void IsHoldExpired_AfterHoldWindow_ReturnsTrue()
    {
        var booking = Create();

        booking.IsHoldExpired(Now.AddMinutes(11)).Should().BeTrue();
        booking.IsHoldExpired(Now.AddMinutes(5)).Should().BeFalse();
    }
}
