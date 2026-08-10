using AuthService.Application.Common.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Auth.Otp;

public sealed record VerifyOtpCommand(Guid UserId, string Code, string Channel, string? IpAddress, string? UserAgent) : IRequest;
