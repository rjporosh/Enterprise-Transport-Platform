using BookingService.Application.Features.Trips.CreateTrip;
using BookingService.Application.Features.Trips.GetTripById;
using BookingService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookingService.UnitTests.Trips;

public class CreateTripHandlerTests : IDisposable
{
    private readonly TestBookingDbContext _context;
    private readonly FakeCacheService _cache = new();

    public CreateTripHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestBookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestBookingDbContext(options);
    }

    private static CreateTripCommand Command(int seats = 8) => new(
        RouteId: Guid.NewGuid(),
        BusId: Guid.NewGuid(),
        OriginCity: "Dhaka",
        DestinationCity: "Chattogram",
        DistanceKm: 264,
        OperatorId: Guid.NewGuid(),
        BusPlateNumber: "DHA-KA-11-2345",
        BusType: "AC Business",
        TotalSeats: seats,
        DepartureUtc: DateTimeOffset.UtcNow.AddDays(7),
        ArrivalUtc: DateTimeOffset.UtcNow.AddDays(7).AddHours(6),
        BasePrice: 1200,
        Currency: "bdt");

    [Fact]
    public async Task Handle_GeneratesSeatInventory_AndUpsertsRouteAndBusReplicas()
    {
        var command = Command(seats: 6);
        var result = await new CreateTripHandler(_context, _cache).Handle(command, CancellationToken.None);

        result.TotalSeats.Should().Be(6);
        result.Currency.Should().Be("BDT");

        var trip = await _context.Trips.Include(t => t.Seats).FirstAsync(t => t.Id == result.TripId);
        trip.Seats.Select(s => s.SeatNumber).Should().BeEquivalentTo(new[] { "1A", "1B", "1C", "1D", "2A", "2B" });

        (await _context.Routes.FindAsync(command.RouteId)).Should().NotBeNull();
        (await _context.Buses.FindAsync(command.BusId)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTripById_ReturnsSeatMapWithEverySeatAvailable()
    {
        var created = await new CreateTripHandler(_context, _cache).Handle(Command(seats: 4), CancellationToken.None);

        var detail = await new GetTripByIdHandler(_context).Handle(new GetTripByIdQuery(created.TripId), CancellationToken.None);

        detail.Seats.Should().HaveCount(4);
        detail.AvailableSeats.Should().Be(4);
        detail.OriginCity.Should().Be("Dhaka");
    }

    public void Dispose() => _context.Dispose();
}
