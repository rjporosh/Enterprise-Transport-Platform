using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Templates.CreateTemplate;

namespace NotificationService.Application.Features.Templates.GetTemplateById;

public sealed record GetTemplateByIdQuery(Guid TemplateId) : IRequest<Result<TemplateDto>>;
