using RouteService.Application.Features.Schedules.UpdateSchedule;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Schedules;

public class UpdateScheduleHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    public UpdateScheduleHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ReturnsUpdatedSchedule()
    {
        var route = Route.Create(Guid.NewGuid(), "SCH-1", "Test Route", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), _clock.UtcNow);
        _context.Routes.Add(route);
        var schedule = Schedule.Create(Guid.NewGuid(), route.Id, TimeSpan.FromHours(8), TimeSpan.FromHours(10), _clock.UtcNow, null, _clock.UtcNow);
        _context.Schedules.Add(schedule);
        _context.SaveChanges();

        var handler = new UpdateScheduleHandler(_context, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<UpdateScheduleHandler>());
        var result = await handler.Handle(new UpdateScheduleCommand(schedule.Id, TimeSpan.FromHours(9), TimeSpan.FromHours(11), null, schedule.Version, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DepartureTime.Should().Be(TimeSpan.FromHours(9));
    }

    [Fact]
    public async Task Handle_VersionMismatch_ReturnsFailure()
    {
        var route = Route.Create(Guid.NewGuid(), "SCH-1", "Test Route", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), _clock.UtcNow);
        _context.Routes.Add(route);
        var schedule = Schedule.Create(Guid.NewGuid(), route.Id, TimeSpan.FromHours(8), TimeSpan.FromHours(10), _clock.UtcNow, null, _clock.UtcNow);
        _context.Schedules.Add(schedule);
        _context.SaveChanges();

        var handler = new UpdateScheduleHandler(_context, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<UpdateScheduleHandler>());
        var result = await handler.Handle(new UpdateScheduleCommand(schedule.Id, TimeSpan.FromHours(9), TimeSpan.FromHours(11), null, schedule.Version + 1, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Any(e => e.Code == "ConcurrencyConflict").Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
