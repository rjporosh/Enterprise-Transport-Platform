using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.Otp;

public sealed class RequestOtpHandler : IRequestHandler<RequestOtpCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<RequestOtpHandler> _logger;

    public RequestOtpHandler(IAuthDbContext context, IOtpService otpService, IAuditLogger auditLogger, ILogger<RequestOtpHandler> logger)
    {
        _context = context;
        _otpService = otpService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            throw new AuthService.Domain.Exceptions.UserNotFoundException(request.UserId);

        await _otpService.GenerateAndSendOtpAsync(request.UserId, request.Channel, request.Destination, request.IpAddress, cancellationToken);
        await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.OtpRequested, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
        _logger.LogInformation("OTP requested for user {UserId} via {Channel}", request.UserId, request.Channel);
    }
}
