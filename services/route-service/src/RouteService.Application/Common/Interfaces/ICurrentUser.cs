namespace RouteService.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
}
