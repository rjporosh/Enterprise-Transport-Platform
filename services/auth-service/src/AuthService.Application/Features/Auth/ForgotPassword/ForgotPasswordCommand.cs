using AuthService.Application.Common.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email, string? IpAddress, string? UserAgent) : IRequest;
