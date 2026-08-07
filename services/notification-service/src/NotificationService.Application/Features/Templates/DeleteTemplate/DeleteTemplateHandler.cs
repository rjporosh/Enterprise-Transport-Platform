using MediatR;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;

namespace NotificationService.Application.Features.Templates.DeleteTemplate;

/// <summary>Soft delete only (CLAUDE.md, "Soft Delete") — templates are referenced by TemplateId from historical Notification rows, so a hard delete would orphan that audit trail.</summary>
public sealed class DeleteTemplateHandler : IRequestHandler<DeleteTemplateCommand, Result>
{
    private readonly INotificationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteTemplateHandler(INotificationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _dbContext.NotificationTemplates.FindAsync(new object?[] { request.TemplateId }, cancellationToken);
        if (template is null || template.IsDeleted)
            return Result.Failure(Error.NotFound($"Template '{request.TemplateId}' was not found."));

        template.SoftDelete(_dateTimeProvider.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
