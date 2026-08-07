using MediatR;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Templates.CreateTemplate;

namespace NotificationService.Application.Features.Templates.GetTemplateById;

public sealed class GetTemplateByIdHandler : IRequestHandler<GetTemplateByIdQuery, Result<TemplateDto>>
{
    private readonly INotificationDbContext _dbContext;

    public GetTemplateByIdHandler(INotificationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<TemplateDto>> Handle(GetTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _dbContext.NotificationTemplates.FindAsync(new object?[] { request.TemplateId }, cancellationToken);
        if (template is null || template.IsDeleted)
            return Result<TemplateDto>.Failure(Error.NotFound($"Template '{request.TemplateId}' was not found."));

        return Result<TemplateDto>.Success(CreateTemplateHandler.ToDto(template));
    }
}
