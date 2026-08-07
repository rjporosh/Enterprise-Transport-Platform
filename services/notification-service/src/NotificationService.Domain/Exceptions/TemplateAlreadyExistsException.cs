namespace NotificationService.Domain.Exceptions;

public sealed class TemplateAlreadyExistsException : DomainException
{
    public TemplateAlreadyExistsException(string templateKey, string locale)
        : base($"A template with key '{templateKey}' already exists for locale '{locale}'.") { }
}
