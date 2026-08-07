namespace NotificationService.Application.Common.Interfaces;

/// <summary>Renders a template source string against a variable bag (Scriban {{var}} syntax — see Infrastructure/Templating/ScribanTemplateRenderer). Kept string-in/string-out at the Application boundary so the concrete templating engine stays an Infrastructure concern.</summary>
public interface ITemplateRenderer
{
    /// <exception cref="NotificationService.Domain.Exceptions.TemplateRenderException">The template source is malformed or references an undefined variable in strict mode.</exception>
    string Render(string templateSource, IReadOnlyDictionary<string, object?> variables);
}
