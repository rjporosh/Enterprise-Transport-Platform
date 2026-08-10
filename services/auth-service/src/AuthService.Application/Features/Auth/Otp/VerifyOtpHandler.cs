using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.Otp;

public sealed class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<VerifyOtpHandler> _logger;

    public VerifyOtpHandler(IAuthDbContext context, IOtpService otpService, IAuditLogger auditLogger, ILogger<VerifyOtpHandler> logger)
    {
        _context = context;
        _otpService = otpService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            throw new AuthService.Domain.Exceptions.UserNotFoundException(request.UserId);

        var isValid = await _otpService.VerifyOtpAsync(request.UserId, request.Code, request.Channel, cancellationToken);
        if (!isValid)
        {
            await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.OtpFailed, user.Id, user.Email, success: false, request.IpAddress, request.UserAgent, "Invalid OTP.", cancellationToken);
            throw new AuthService.Domain.Exceptions.InvalidOtpException();
        }

        await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.OtpVerified, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
        _logger.LogInformation("OTP verified for user {UserId}", request.UserId);
    }
}
