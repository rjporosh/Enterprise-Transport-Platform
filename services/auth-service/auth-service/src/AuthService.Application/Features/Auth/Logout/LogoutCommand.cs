using MediatR;

namespace AuthService.Application.Features.Auth.Logout;

public sealed record LogoutCommand(string RawRefreshToken, string? IpAddress, string? UserAgent) : IRequest;
