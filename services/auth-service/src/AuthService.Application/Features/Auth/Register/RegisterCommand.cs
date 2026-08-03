using AuthService.Application.Common.Models;
using MediatR;

namespace AuthService.Application.Features.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? IpAddress,
    string? UserAgent) : IRequest<TokenPairDto>;
