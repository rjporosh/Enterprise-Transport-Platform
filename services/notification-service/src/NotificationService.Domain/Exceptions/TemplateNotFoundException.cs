namespace NotificationService.Domain.Exceptions;

public sealed class TemplateNotFoundException : DomainException
{
    public TemplateNotFoundException(Guid templateId)
        : base($"Notification template {templateId} was not found.") { }

    public TemplateNotFoundException(string templateKey, string locale)
        : base($"Notification template {templateKey} for locale {locale} was not found.") { }
}
