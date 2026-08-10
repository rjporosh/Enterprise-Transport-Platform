using RouteService.Application.Common.Interfaces;

namespace RouteService.UnitTests.TestSupport;

public sealed class FakeCurrentUser : ICurrentUser
{
    public string? UserId { get; set; } = "test-user";
    public IReadOnlyCollection<string> Roles { get; set; } = new[] { "Admin" };
}
