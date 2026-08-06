using BusService.Application.Features.Buses.ChangeBusStatus;
using BusService.Domain.Entities;
using BusService.Domain.Enums;
using BusService.Domain.Exceptions;
using BusService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusService.UnitTests.Buses;

public class ChangeBusStatusHandlerTests : IDisposable
{
    private readonly TestBusDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeBusMetrics _metrics = new();
    private readonly FakeCacheService _cache = new();
    private Bus _bus = default!;

    public ChangeBusStatusHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestBusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestBusDbContext(options);

        var depotId = Guid.NewGuid();
        _context.Depots.Add(Depot.Create(depotId, "Central Depot", "Dhaka", null));

        _bus = Bus.Register(Guid.NewGuid(), Guid.NewGuid(), "DL-1PC-1234", BusType.AcSleeper, 40, depotId, null, null, null, _clock.UtcNow);
        _bus.ClearDomainEvents();
        _context.Buses.Add(_bus);
        _context.SaveChanges();
    }

    private ChangeBusStatusHandler CreateHandler() => new(_context, _eventPublisher, _clock, _metrics, _cache);

    [Fact]
    public async Task Handle_WithValidTransition_UpdatesStatus_AndRecordsMetric()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeBusStatusCommand(_bus.Id, "UnderMaintenance"), CancellationToken.None);

        result.Status.Should().Be("UnderMaintenance");
        _metrics.StatusChanges.Should().ContainSingle(s => s == "UnderMaintenance");

        var reloaded = await _context.Buses.FirstAsync(b => b.Id == _bus.Id);
        reloaded.Status.Should().Be(BusStatus.UnderMaintenance);
    }

    [Fact]
    public async Task Handle_WithInvalidTransition_ThrowsInvalidBusStatusTransitionException()
    {
        var handler = CreateHandler();
        await handler.Handle(new ChangeBusStatusCommand(_bus.Id, "Retired"), CancellationToken.None);

        var act = async () => await handler.Handle(new ChangeBusStatusCommand(_bus.Id, "Active"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidBusStatusTransitionException>();
    }

    [Fact]
    public async Task Handle_WithUnknownBus_ThrowsBusNotFoundException()
    {
        var handler = CreateHandler();

        var act = async () => await handler.Handle(new ChangeBusStatusCommand(Guid.NewGuid(), "Retired"), CancellationToken.None);

        await act.Should().ThrowAsync<BusNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
