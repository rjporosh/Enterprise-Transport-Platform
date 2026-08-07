using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.Templates.CreateTemplate;

public sealed class CreateTemplateHandler : IRequestHandler<CreateTemplateCommand, Result<TemplateDto>>
{
    private readonly INotificationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTemplateHandler(INotificationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TemplateDto>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        var key = request.Key.Trim().ToLowerInvariant();
        var locale = request.Locale.Trim().ToLowerInvariant();

        var exists = await _dbContext.NotificationTemplates.AsNoTracking()
            .AnyAsync(t => t.Key == key && t.Channel == request.Channel && t.Locale == locale && !t.IsDeleted, cancellationToken);
        if (exists)
            return Result<TemplateDto>.Failure(Error.Conflict(
                $"A template with key '{key}' already exists for channel '{request.Channel}' and locale '{locale}'."));

        var template = NotificationTemplate.Create(
            key, request.Channel, locale, request.Name, request.Description,
            request.Subject, request.Body, request.DataPayloadTemplate, _dateTimeProvider.UtcNow);

        _dbContext.NotificationTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<TemplateDto>.Success(ToDto(template));
    }

    internal static TemplateDto ToDto(NotificationTemplate t) => new(
        t.Id, t.Key, t.Channel, t.Locale, t.Name, t.Description, t.Subject, t.Body,
        t.DataPayloadTemplate, t.IsActive, t.Version, t.CreatedAtUtc, t.UpdatedAtUtc);
}
