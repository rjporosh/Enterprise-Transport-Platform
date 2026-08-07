using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Notifications.GetNotificationById;

namespace NotificationService.Application.Features.Notifications.GetNotifications;

public sealed class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    private readonly INotificationDbContext _dbContext;

    public GetNotificationsHandler(INotificationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PagedResult<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Notifications.AsNoTracking().Where(n => !n.IsDeleted);

        if (request.Channel is not null) query = query.Where(n => n.Channel == request.Channel);
        if (request.Status is not null) query = query.Where(n => n.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Recipient)) query = query.Where(n => n.Recipient == request.Recipient);
        if (!string.IsNullOrWhiteSpace(request.SourceReference)) query = query.Where(n => n.SourceReference == request.SourceReference);
        if (request.CreatedFromUtc is not null) query = query.Where(n => n.CreatedAtUtc >= request.CreatedFromUtc);
        if (request.CreatedToUtc is not null) query = query.Where(n => n.CreatedAtUtc <= request.CreatedToUtc);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Deliberately NOT EF.Functions.ILike here: that'\''s a Postgres-only
            // (Npgsql) translation and would throw at runtime under
            // Database:Provider=SqlServer|MySql. ToLower().Contains() is the
            // one search pattern EF Core translates identically across all
            // three wired providers, which is the point of the provider
            // switch in the first place — see docs/architecture, "Database
            // portability". Trade-off: no case-insensitive index usage on
            // Postgres; acceptable at this table'\''s expected volume, revisit
            // with a trigram/full-text index if search becomes a hot path.
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(n =>
                n.Recipient.ToLower().Contains(term) ||
                (n.Subject != null && n.Subject.ToLower().Contains(term)) ||
                (n.SourceReference != null && n.SourceReference.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Recipient, n.Channel, n.Status, n.Priority, n.Subject, n.Body, n.SourceReference, n.Locale,
                n.ScheduledForUtc, n.SentAtUtc, n.DeliveredAtUtc, n.RetryCount, n.MaxRetryCount, n.NextRetryAtUtc,
                n.LastError, n.CreatedAtUtc, n.UpdatedAtUtc, Array.Empty<NotificationLogDto>()))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<NotificationDto>>.Success(
            new PagedResult<NotificationDto>(items, totalCount, request.Page, request.PageSize));
    }
}
