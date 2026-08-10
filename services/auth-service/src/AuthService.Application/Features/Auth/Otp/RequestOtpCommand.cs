using AuthService.Application.Common.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Auth.Otp;

public sealed record RequestOtpCommand(Guid UserId, string Channel, string Destination, string? IpAddress, string? UserAgent) : IRequest;
