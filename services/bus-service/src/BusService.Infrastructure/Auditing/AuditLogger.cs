using BusService.Application.Common.Interfaces;
using BusService.Domain.Entities;
using BusService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusService.Infrastructure.Auditing;

public sealed class AuditLogger : IAuditLogger
{
    private readonly BusDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AuditLogger(BusDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task LogAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        log.UserId ??= _currentUser.UserId;
        log.Timestamp = DateTimeOffset.UtcNow;
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
