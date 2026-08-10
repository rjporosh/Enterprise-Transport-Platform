using RouteService.Application.Features.Schedules.GetSchedule;
using RouteService.Domain.Entities;
using RouteService.Domain.Exceptions;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Schedules;

public class GetScheduleHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();

    public GetScheduleHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);

        var route = Route.Create(Guid.NewGuid(), "SCH-1", "Test Route", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), _clock.UtcNow);
        _context.Routes.Add(route);
        _context.Schedules.Add(Schedule.Create(Guid.NewGuid(), route.Id, TimeSpan.FromHours(8), TimeSpan.FromHours(10), _clock.UtcNow, null, _clock.UtcNow));
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_ExistingSchedule_ReturnsSchedule()
    {
        var handler = new GetScheduleHandler(_context);
        var schedule = _context.Schedules.First();
        var result = await handler.Handle(new GetScheduleQuery(schedule.Id), CancellationToken.None);
        result.RouteId.Should().Be(schedule.RouteId);
    }

    [Fact]
    public async Task Handle_MissingSchedule_ThrowsNotFoundException()
    {
        var handler = new GetScheduleHandler(_context);
        var act = async () => await handler.Handle(new GetScheduleQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<ScheduleNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
