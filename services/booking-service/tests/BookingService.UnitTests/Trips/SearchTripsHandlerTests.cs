using BookingService.Application.Features.Trips.SearchTrips;
using BookingService.Domain.Common;
using BookingService.Domain.Entities;
using BookingService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookingService.UnitTests.Trips;

public class SearchTripsHandlerTests : IDisposable
{
    private readonly TestBookingDbContext _context;
    private static readonly DateOnly DepartureDate = new(2026, 8, 15);

    public SearchTripsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestBookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestBookingDbContext(options);

        var route = new Route(Guid.NewGuid(), "Dhaka", "Chattogram", 264m);
        var bus = new Bus(Guid.NewGuid(), Guid.NewGuid(), "DHK-1234", "AC Sleeper", 3);
        var departure = new DateTimeOffset(DepartureDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(8))), TimeSpan.Zero);

        var trip = new Trip(
            Guid.NewGuid(), route.Id, bus.Id, departure, departure.AddHours(6),
            new Money(1500m, "BDT"),
            new[] { ("A1", "Lower"), ("A2", "Lower"), ("A3", "Lower") });

        trip.HoldSeats(new[] { "A1" }); // one seat taken, two should remain available

        _context.Routes.Add(route);
        _context.Buses.Add(bus);
        _context.Trips.Add(trip);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_ReturnsMatchingTrip_WithCorrectAvailableSeatCount()
    {
        var handler = new SearchTripsHandler(_context);
        var query = new SearchTripsQuery("Dhaka", "Chattogram", DepartureDate);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        var trip = result.Items.Single();
        trip.OriginCity.Should().Be("Dhaka");
        trip.DestinationCity.Should().Be("Chattogram");
        trip.AvailableSeats.Should().Be(2);
        trip.PricePerSeat.Should().Be(1500m);
    }

    [Fact]
    public async Task Handle_WhenNoRouteMatches_ReturnsEmptyResult()
    {
        var handler = new SearchTripsHandler(_context);
        var query = new SearchTripsQuery("Dhaka", "Sylhet", DepartureDate);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
