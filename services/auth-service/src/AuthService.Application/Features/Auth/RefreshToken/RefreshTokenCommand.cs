using AuthService.Application.Common.Models;
using MediatR;

namespace AuthService.Application.Features.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RawRefreshToken, string? IpAddress, string? UserAgent) : IRequest<TokenPairDto>;
