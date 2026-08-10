using RouteService.Application.Features.Schedules.CreateSchedule;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Schedules;

public class CreateScheduleHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    public CreateScheduleHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);

        var route = Route.Create(Guid.NewGuid(), "SCH-1", "Test Route", Guid.NewGuid(), Guid.NewGuid(), RouteService.Domain.Enums.TransportMode.Bus, 100, TimeSpan.FromHours(2), _clock.UtcNow);
        _context.Routes.Add(route);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsScheduleDto()
    {
        var route = _context.Routes.First();
        var handler = new CreateScheduleHandler(_context, _eventPublisher, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<CreateScheduleHandler>());
        var command = new CreateScheduleCommand(route.Id, TimeSpan.FromHours(8), TimeSpan.FromHours(14), DateTimeOffset.UtcNow, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RouteId.Should().Be(route.Id);
    }

    public void Dispose() => _context.Dispose();
}
