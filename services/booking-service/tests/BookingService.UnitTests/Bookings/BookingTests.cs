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

    [Fact]
    public void Create_SetsStatusToPendingPayment_AndComputesTotalFromSeatCount()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(500m, "BDT"),
            new[]
            {
                ("A1", "Porosh Ahmed", 30, "Male"),
                ("A2", "Jane Doe", 27, "Female")
            }, Now);

        booking.Status.Should().Be(BookingStatus.PendingPayment);
        booking.TotalAmount.Amount.Should().Be(1000m);
        booking.Seats.Should().HaveCount(2);
        booking.HoldExpiresAtUtc.Should().Be(Now.AddMinutes(10));
    }

    [Fact]
    public void Create_RaisesBookingCreatedDomainEvent()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(500m, "BDT"), OnePassenger, Now);

        booking.DomainEvents.Should().ContainSingle(e => e is BookingCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithNoPassengers_ThrowsInvalidBookingStateException()
    {
        var act = () => Booking.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(500m, "BDT"),
            Array.Empty<(string, string, int, string)>(), Now);

        act.Should().Throw<InvalidBookingStateException>();
    }

    [Fact]
    public void Confirm_FromPendingPayment_SetsConfirmedAndRaisesEvent()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(500m, "BDT"), OnePassenger, Now);
        booking.ClearDomainEvents();

        booking.Confirm(Now.AddMinutes(2));

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedAtUtc.Should().Be(Now.AddMinutes(2));
        booking.DomainEvents.Should().ContainSingle(e => e is BookingConfirmedDomainEvent);
    }

    [Fact]
    public void Confirm_WhenAlreadyCancelled_ThrowsInvalidBookingStateException()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(500m, "BDT"), OnePassenger, Now);
        booking.Cancel("Changed my mind", Now.AddMinutes(1));

        var act = () => booking.Confirm(Now.AddMinutes(2));

        act.Should().Throw<InvalidBookingStateException>();
    }

    [Fact]
    public void Cancel_ReleasesReasonAndRaisesEvent()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(500m, "BDT"), OnePassenger, Now);
        booking.ClearDomainEvents();

        booking.Cancel("Customer requested refund", Now.AddMinutes(3));

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("Customer requested refund");
        booking.DomainEvents.Should().ContainSingle(e => e is BookingCancelledDomainEvent);
    }

    [Fact]
    public void IsHoldExpired_AfterHoldWindow_ReturnsTrue()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(500m, "BDT"), OnePassenger, Now);

        booking.IsHoldExpired(Now.AddMinutes(11)).Should().BeTrue();
        booking.IsHoldExpired(Now.AddMinutes(5)).Should().BeFalse();
    }
}
