using AuthService.Application.Common.Models;
using MediatR;

namespace AuthService.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password, string? IpAddress, string? UserAgent) : IRequest<TokenPairDto>;
