using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Templates.CreateTemplate;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Templates.GetTemplates;

public sealed record GetTemplatesQuery(
    int Page, int PageSize, TemplateChannel? Channel, string? Locale, bool? IsActive, string? Search)
    : IRequest<Result<PagedResult<TemplateDto>>>;
