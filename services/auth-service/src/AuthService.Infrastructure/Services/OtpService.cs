using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AuthService.Infrastructure.Services;

public sealed class OtpService : IOtpService
{
    private readonly IAuthDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<OtpService> _logger;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private const int MaxResendsPerHour = 3;

    public OtpService(IAuthDbContext context, IEmailSender emailSender, ISmsSender smsSender, IDateTimeProvider clock, ILogger<OtpService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _clock = clock;
        _logger = logger;
    }

    public async Task<string> GenerateAndSendOtpAsync(Guid userId, string channel, string destination, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var oneHourAgo = now.AddHours(-1);

        var recentCount = await _context.OtpRecords.CountAsync(o => o.UserId == userId && o.Channel == channel && o.CreatedAtUtc >= oneHourAgo, cancellationToken);
        if (recentCount >= MaxResendsPerHour)
            throw new AuthService.Domain.Exceptions.OtpRateLimitExceededException();

        var existingUnused = await _context.OtpRecords
            .Where(o => o.UserId == userId && o.Channel == channel && !o.IsUsed && o.ExpiresAtUtc > now)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUnused is not null)
        {
            existingUnused.IncrementResend();
            await _context.SaveChangesAsync(cancellationToken);
            return destination;
        }

        var code = GenerateCode();
        var codeHash = HashCode(code);

        var otp = new OtpRecord(Guid.NewGuid(), userId, codeHash, channel, destination, now, OtpLifetime, ipAddress);
        _context.OtpRecords.Add(otp);
        await _context.SaveChangesAsync(cancellationToken);

        var message = $"Your verification code is: {code}. Valid for {OtpLifetime.TotalMinutes} minutes.";
        if (channel == "email")
            await _emailSender.SendAsync(destination, "Verification Code", message, cancellationToken);
        else if (channel == "sms")
            await _smsSender.SendAsync(destination, message, cancellationToken);

        return destination;
    }

    public async Task<bool> VerifyOtpAsync(Guid userId, string code, string channel, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var codeHash = HashCode(code);

        var otp = await _context.OtpRecords
            .Where(o => o.UserId == userId && o.Channel == channel && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || !otp.CanAttempt(now, MaxAttempts))
        {
            if (otp is not null && otp.IsExpired(now))
                throw new AuthService.Domain.Exceptions.OtpExpiredException();
            throw new AuthService.Domain.Exceptions.InvalidOtpException();
        }

        otp.MarkAttempt();
        var isValid = string.Equals(otp.CodeHash, codeHash, StringComparison.Ordinal);

        if (isValid)
        {
            otp.MarkVerified(now);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        if (otp.AttemptCount >= MaxAttempts)
            throw new AuthService.Domain.Exceptions.OtpRateLimitExceededException();
        throw new AuthService.Domain.Exceptions.InvalidOtpException();
    }

    public async Task CleanupExpiredOtpsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = _clock.UtcNow.AddDays(-30);
        var expired = await _context.OtpRecords
            .Where(o => o.CreatedAtUtc < cutoff)
            .ToListAsync(cancellationToken);
        if (expired.Count > 0)
        {
            _context.OtpRecords.RemoveRange(expired);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} expired OTP records.", expired.Count);
        }
    }

    private static string GenerateCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes) % 1000000;
        return value.ToString("D6");
    }

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(code);
        return Convert.ToHexString(sha256.ComputeHash(bytes));
    }
}
