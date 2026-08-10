using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Templates.CreateTemplate;

public sealed record TemplateDto(
    Guid Id, string Key, TemplateChannel Channel, string Locale, string Name, string? Description,
    string? Subject, string Body, string? DataPayloadTemplate, bool IsActive, int Version,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
