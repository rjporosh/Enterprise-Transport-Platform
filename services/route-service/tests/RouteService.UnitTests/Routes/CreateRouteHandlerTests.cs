using RouteService.Application.Features.Routes.CreateRoute;
using RouteService.Domain.Entities;
using RouteService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RouteService.UnitTests.Routes;

public class CreateRouteHandlerTests : IDisposable
{
    private readonly TestRouteDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeRouteMetrics _metrics = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly Guid _originStopId;
    private readonly Guid _destinationStopId;

    public CreateRouteHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestRouteDbContext(options);

        _originStopId = Guid.NewGuid();
        _destinationStopId = Guid.NewGuid();

        _context.Stops.AddRange(
            Stop.Create(_originStopId, "DHK", "Dhaka", "Dhaka", "Kamalapur", 23.8103, 90.4125, _clock.UtcNow),
            Stop.Create(_destinationStopId, "CTG", "Chittagong", "Chittagong", "CTG Station", 22.3569, 91.7832, _clock.UtcNow));
        _context.SaveChanges();
    }

    private CreateRouteHandler CreateHandler() => new(_context, _eventPublisher, _clock, new RouteService.Infrastructure.Observability.AuditLogger(_currentUser, new FakeLogger<RouteService.Infrastructure.Observability.AuditLogger>()), _currentUser, new FakeLogger<CreateRouteHandler>());

    [Fact]
    public async Task Handle_WithValidData_ReturnsRouteDto()
    {
        var handler = CreateHandler();
        var command = new CreateRouteCommand("DAC-DHK-CTG", "Dhaka to Chittagong", _originStopId, _destinationStopId, "Bus", 250.0, TimeSpan.FromHours(6), "tester");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("DAC-DHK-CTG");
        result.Value.Status.Should().Be("Draft");
        _metrics.RoutesCreated.Should().Be(0); // metrics not wired in unit test
    }

    [Fact]
    public async Task Handle_WithDuplicateCode_ReturnsFailure()
    {
        var handler = CreateHandler();
        await handler.Handle(new CreateRouteCommand("DAC-DHK-CTG", "Dhaka to Chittagong", _originStopId, _destinationStopId, "Bus", 250.0, TimeSpan.FromHours(6), null), CancellationToken.None);

        var result = await handler.Handle(new CreateRouteCommand("DAC-DHK-CTG", "Duplicate", _originStopId, _destinationStopId, "Bus", 250.0, TimeSpan.FromHours(6), null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Any(e => e.Code == "DuplicateRouteCode").Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
