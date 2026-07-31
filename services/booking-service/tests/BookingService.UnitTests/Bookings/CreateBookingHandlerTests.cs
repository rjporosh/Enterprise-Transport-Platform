using BookingService.Application.Features.Bookings.CreateBooking;
using BookingService.Domain.Common;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using BookingService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BookingService.UnitTests.Bookings;

public class CreateBookingHandlerTests : IDisposable
{
    private readonly TestBookingDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly Guid _tripId = Guid.NewGuid();

    public CreateBookingHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestBookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestBookingDbContext(options);

        var trip = new Trip(
            _tripId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            _clock.UtcNow.AddHours(4),
            _clock.UtcNow.AddHours(8),
            new Money(800m, "BDT"),
            new[] { ("A1", "Lower"), ("A2", "Lower"), ("A3", "Lower") });

        _context.Trips.Add(trip);
        _context.SaveChanges();
    }

    private CreateBookingHandler CreateHandler() =>
        new(_context, _eventPublisher, _clock, NullLogger<CreateBookingHandler>.Instance);

    [Fact]
    public async Task Handle_WithAvailableSeats_CreatesBooking_HoldsSeats_AndEnqueuesOutboxEvent()
    {
        var handler = CreateHandler();
        var command = new CreateBookingCommand(_tripId, Guid.NewGuid(),
            new[] { new PassengerDto("A1", "Porosh Ahmed", 30, "Male") });

        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(Domain.Enums.BookingStatus.PendingPayment);
        result.TotalAmount.Should().Be(800m);

        var trip = await _context.Trips.Include(t => t.Seats).FirstAsync(t => t.Id == _tripId);
        trip.Seats.Single(s => s.SeatNumber == "A1").Status.Should().Be(Domain.Enums.SeatStatus.Held);

        _eventPublisher.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenSeatAlreadyBooked_ThrowsSeatUnavailableException()
    {
        var firstHandler = CreateHandler();
        await firstHandler.Handle(
            new CreateBookingCommand(_tripId, Guid.NewGuid(), new[] { new PassengerDto("A1", "First Customer", 30, "Male") }),
            CancellationToken.None);

        var secondHandler = CreateHandler();
        var act = () => secondHandler.Handle(
            new CreateBookingCommand(_tripId, Guid.NewGuid(), new[] { new PassengerDto("A1", "Second Customer", 25, "Female") }),
            CancellationToken.None);

        await act.Should().ThrowAsync<SeatUnavailableException>();
    }

    [Fact]
    public async Task Handle_WhenTripDoesNotExist_ThrowsTripNotFoundException()
    {
        var handler = CreateHandler();
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(),
            new[] { new PassengerDto("A1", "Porosh Ahmed", 30, "Male") });

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<TripNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
