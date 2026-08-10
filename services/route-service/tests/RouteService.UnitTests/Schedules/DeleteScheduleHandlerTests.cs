using RouteService.Application.Features.Schedules.DeleteSchedule;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Schedules;

public class DeleteScheduleHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    public DeleteScheduleHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);
    }

    [Fact]
    public async Task Handle_SoftDeletesSchedule()
    {
        var route = Route.Create(Guid.NewGuid(), "SCH-1", "Test Route", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), _clock.UtcNow);
        _context.Routes.Add(route);
        var schedule = Schedule.Create(Guid.NewGuid(), route.Id, TimeSpan.FromHours(8), TimeSpan.FromHours(10), _clock.UtcNow, null, _clock.UtcNow);
        _context.Schedules.Add(schedule);
        _context.SaveChanges();

        var handler = new DeleteScheduleHandler(_context, new FakeEventPublisher(), _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<DeleteScheduleHandler>());
        var result = await handler.Handle(new DeleteScheduleCommand(schedule.Id, schedule.Version), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.IsDeleted.Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
