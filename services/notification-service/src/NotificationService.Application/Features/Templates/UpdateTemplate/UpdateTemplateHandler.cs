using MediatR;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Templates.CreateTemplate;

namespace NotificationService.Application.Features.Templates.UpdateTemplate;

public sealed class UpdateTemplateHandler : IRequestHandler<UpdateTemplateCommand, Result<TemplateDto>>
{
    private readonly INotificationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateTemplateHandler(INotificationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TemplateDto>> Handle(UpdateTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _dbContext.NotificationTemplates.FindAsync(new object?[] { request.TemplateId }, cancellationToken);
        if (template is null || template.IsDeleted)
            return Result<TemplateDto>.Failure(Error.NotFound($"Template '{request.TemplateId}' was not found."));

        var nowUtc = _dateTimeProvider.UtcNow;
        template.Update(request.Name, request.Description, request.Subject, request.Body, request.DataPayloadTemplate, nowUtc);

        if (request.IsActive) template.Activate(nowUtc);
        else template.Deactivate(nowUtc);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<TemplateDto>.Success(CreateTemplateHandler.ToDto(template));
    }
}
