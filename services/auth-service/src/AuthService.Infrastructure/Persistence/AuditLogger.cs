using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Persistence;

/// <summary>
/// Best-effort by design: a failure to write an audit row must never fail
/// the request it is describing (e.g. a DB blip shouldn't block a real
/// login) — the failure is logged instead so it still surfaces in
/// Prometheus/Grafana via log-based alerting.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly AuthDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(AuthDbContext context, IDateTimeProvider clock, ILogger<AuditLogger> logger)
    {
        _context = context;
        _clock = clock;
        _logger = logger;
    }

    public async Task LogAsync(
        AuditAction action,
        Guid? userId,
        string? email,
        bool success,
        string? ipAddress,
        string? userAgent,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = AuditLog.Create(action, userId, email, success, ipAddress, userAgent, details, _clock.UtcNow);
            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry for {Action} (user {UserId})", action, userId);
        }
    }
}
