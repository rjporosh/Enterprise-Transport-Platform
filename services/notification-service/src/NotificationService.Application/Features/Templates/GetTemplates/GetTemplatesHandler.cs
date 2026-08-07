using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Templates.CreateTemplate;

namespace NotificationService.Application.Features.Templates.GetTemplates;

public sealed class GetTemplatesHandler : IRequestHandler<GetTemplatesQuery, Result<PagedResult<TemplateDto>>>
{
    private readonly INotificationDbContext _dbContext;

    public GetTemplatesHandler(INotificationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PagedResult<TemplateDto>>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.NotificationTemplates.AsNoTracking().Where(t => !t.IsDeleted);

        if (request.Channel is not null) query = query.Where(t => t.Channel == request.Channel);
        if (!string.IsNullOrWhiteSpace(request.Locale)) query = query.Where(t => t.Locale == request.Locale);
        if (request.IsActive is not null) query = query.Where(t => t.IsActive == request.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(t => t.Key.ToLower().Contains(term) || t.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Key).ThenBy(t => t.Locale)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TemplateDto(t.Id, t.Key, t.Channel, t.Locale, t.Name, t.Description, t.Subject, t.Body,
                t.DataPayloadTemplate, t.IsActive, t.Version, t.CreatedAtUtc, t.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<TemplateDto>>.Success(new PagedResult<TemplateDto>(items, totalCount, request.Page, request.PageSize));
    }
}
