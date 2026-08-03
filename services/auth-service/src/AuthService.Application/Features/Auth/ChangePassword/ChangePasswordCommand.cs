using MediatR;

namespace AuthService.Application.Features.Auth.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword, string? IpAddress, string? UserAgent) : IRequest;
