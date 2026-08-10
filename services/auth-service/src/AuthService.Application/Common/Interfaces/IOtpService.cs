namespace AuthService.Application.Common.Interfaces;

public interface IOtpService
{
    Task<string> GenerateAndSendOtpAsync(Guid userId, string channel, string destination, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> VerifyOtpAsync(Guid userId, string code, string channel, CancellationToken cancellationToken = default);
    Task CleanupExpiredOtpsAsync(CancellationToken cancellationToken = default);
}
