using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Templates.CreateTemplate;

namespace NotificationService.Application.Features.Templates.UpdateTemplate;

public sealed record UpdateTemplateCommand(
    Guid TemplateId, string Name, string? Description, string? Subject, string Body,
    string? DataPayloadTemplate, bool IsActive) : IRequest<Result<TemplateDto>>;
