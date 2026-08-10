using AuthService.Application.Common.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword, string? IpAddress, string? UserAgent) : IRequest;
