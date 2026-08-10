using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

public sealed class PasswordHistoryValidator : IPasswordHistoryValidator
{
    private readonly IAuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<PasswordHistoryValidator> _logger;
    private const int DefaultHistoryCount = 3;

    public PasswordHistoryValidator(IAuthDbContext context, IPasswordHasher passwordHasher, ILogger<PasswordHistoryValidator> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<bool> IsPasswordReusedAsync(Guid userId, string plainTextPassword, CancellationToken cancellationToken = default)
    {
        var history = await _context.PasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Take(DefaultHistoryCount)
            .ToListAsync(cancellationToken);

        foreach (var entry in history)
        {
            if (_passwordHasher.Verify(plainTextPassword, entry.PasswordHash))
                return true;
        }
        return false;
    }

    public async Task RecordPasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        var history = new PasswordHistory(Guid.NewGuid(), userId, passwordHash, DateTimeOffset.UtcNow);
        _context.PasswordHistories.Add(history);

        var excess = await _context.PasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Skip(DefaultHistoryCount)
            .ToListAsync(cancellationToken);

        if (excess.Count > 0)
            _context.PasswordHistories.RemoveRange(excess);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
