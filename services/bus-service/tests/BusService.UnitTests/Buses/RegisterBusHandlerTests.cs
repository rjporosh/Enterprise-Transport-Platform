using BusService.Application.Features.Buses.RegisterBus;
using BusService.Application.Common.Interfaces;
using BusService.Domain.Entities;
using BusService.Domain.Exceptions;
using BusService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusService.UnitTests.Buses;

public class RegisterBusHandlerTests : IDisposable
{
    private readonly TestBusDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeBusMetrics _metrics = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly Guid _depotId = Guid.NewGuid();

    public RegisterBusHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestBusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestBusDbContext(options);

        _context.Depots.Add(Depot.Create(_depotId, "Central Depot", "Dhaka", null));
        _context.SaveChanges();
    }

    private RegisterBusHandler CreateHandler() => new(_context, _eventPublisher, _clock, _metrics, _currentUser);

    [Fact]
    public async Task Handle_WithNewPlateNumber_RegistersBus()
    {
        var handler = CreateHandler();
        var command = new RegisterBusCommand(Guid.NewGuid(), "DL-1PC-1234", "AcSleeper", 40, _depotId, "Volvo", "9600", 2022, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.PlateNumber.Should().Be("DL-1PC-1234");
        result.Status.Should().Be("Active");
        _metrics.RegisteredCount.Should().Be(1);
        (await _context.Buses.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithUnknownDepot_ThrowsDepotNotFoundException()
    {
        var handler = CreateHandler();
        var command = new RegisterBusCommand(Guid.NewGuid(), "DL-1PC-1234", "AcSleeper", 40, Guid.NewGuid(), null, null, null, null, null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DepotNotFoundException>();
    }

    [Fact]
    public async Task Handle_WithDuplicatePlateNumber_ThrowsDuplicatePlateNumberException()
    {
        var handler = CreateHandler();
        await handler.Handle(new RegisterBusCommand(Guid.NewGuid(), "DL-1PC-1234", "AcSleeper", 40, _depotId, null, null, null, null, null, null), CancellationToken.None);

        var act = async () => await handler.Handle(new RegisterBusCommand(Guid.NewGuid(), "dl-1pc-1234", "NonAcSeater", 45, _depotId, null, null, null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicatePlateNumberException>();
    }

    public void Dispose() => _context.Dispose();
}
