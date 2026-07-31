using BookingService.Domain.Common;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BookingService.UnitTests.Trips;

public class TripTests
{
    private static Trip CreateTrip(params string[] seatNumbers)
    {
        var layout = seatNumbers.Select(s => (SeatNumber: s, Deck: "Lower"));
        return new Trip(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(2),
            DateTimeOffset.UtcNow.AddHours(6),
            new Money(1200m, "BDT"),
            layout);
    }

    [Fact]
    public void HoldSeats_WhenAllSeatsAvailable_MarksThemHeld()
    {
        var trip = CreateTrip("A1", "A2", "A3");

        trip.HoldSeats(new[] { "A1", "A2" });

        trip.AvailableSeatCount.Should().Be(1);
        trip.Seats.Single(s => s.SeatNumber == "A1").Status.Should().Be(Domain.Enums.SeatStatus.Held);
        trip.Seats.Single(s => s.SeatNumber == "A3").Status.Should().Be(Domain.Enums.SeatStatus.Available);
    }

    [Fact]
    public void HoldSeats_WhenSeatAlreadyHeld_ThrowsSeatUnavailableException_AndLeavesOtherSeatsUnchanged()
    {
        var trip = CreateTrip("A1", "A2", "A3");
        trip.HoldSeats(new[] { "A1" });

        var act = () => trip.HoldSeats(new[] { "A1", "A2" });

        act.Should().Throw<SeatUnavailableException>();
        // A2 must NOT have been held — the whole request fails atomically.
        trip.Seats.Single(s => s.SeatNumber == "A2").Status.Should().Be(Domain.Enums.SeatStatus.Available);
    }

    [Fact]
    public void HoldSeats_WhenSeatDoesNotExist_ThrowsSeatUnavailableException()
    {
        var trip = CreateTrip("A1", "A2");

        var act = () => trip.HoldSeats(new[] { "Z9" });

        act.Should().Throw<SeatUnavailableException>();
    }

    [Fact]
    public void ReleaseSeats_AfterHold_MakesSeatsAvailableAgain()
    {
        var trip = CreateTrip("A1", "A2");
        trip.HoldSeats(new[] { "A1" });

        trip.ReleaseSeats(new[] { "A1" });

        trip.AvailableSeatCount.Should().Be(2);
    }
}
