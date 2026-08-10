namespace AuthService.Application.Common.Interfaces;

public interface IPasswordHistoryValidator
{
    Task<bool> IsPasswordReusedAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
    Task RecordPasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
}
