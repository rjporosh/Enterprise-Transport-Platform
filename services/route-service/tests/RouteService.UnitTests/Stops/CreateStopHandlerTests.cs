using RouteService.Application.Features.Stops.CreateStop;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Stops;

public class CreateStopHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    public CreateStopHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsStopDto()
    {
        var handler = new CreateStopHandler(_context, _eventPublisher, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<CreateStopHandler>());
        var command = new CreateStopCommand("DHK", "Dhaka", "Dhaka", "Kamalapur", 23.8103, 90.4125, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("DHK");
    }

    public void Dispose() => _context.Dispose();
}
