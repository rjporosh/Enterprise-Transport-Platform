using RouteService.Application.Features.Schedules.GetSchedules;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Schedules;

public class GetSchedulesHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();

    public GetSchedulesHandlerTests()
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
    public async Task Handle_ReturnsPagedSchedules()
    {
        var handler = new GetSchedulesHandler(_context);
        var result = await handler.Handle(new GetSchedulesQuery(null, null, 1, 50), CancellationToken.None);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_FilterByRouteId_ReturnsMatching()
    {
        var handler = new GetSchedulesHandler(_context);
        var routeId = _context.Routes.First().Id;
        var result = await handler.Handle(new GetSchedulesQuery(routeId, null, 1, 50), CancellationToken.None);
        result.Items.Should().HaveCount(1);
    }

    public void Dispose() => _context.Dispose();
}
