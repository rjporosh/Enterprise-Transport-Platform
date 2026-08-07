using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Templates.CreateTemplate;

public sealed record CreateTemplateCommand(
    string Key, TemplateChannel Channel, string Locale, string Name, string? Description,
    string? Subject, string Body, string? DataPayloadTemplate) : IRequest<Result<TemplateDto>>;
