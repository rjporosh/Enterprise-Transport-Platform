namespace NotificationService.Domain.Exceptions;

/// <summary>Wraps a templating-engine failure (missing placeholder, malformed
/// template syntax) with the template key so the root cause is obvious from
/// the exception message alone, without needing to attach the raw engine
/// stack trace.</summary>
public sealed class TemplateRenderException : DomainException
{
    public TemplateRenderException(string templateKey, string reason)
        : base($"Failed to render template '{templateKey}': {reason}") { }
}
