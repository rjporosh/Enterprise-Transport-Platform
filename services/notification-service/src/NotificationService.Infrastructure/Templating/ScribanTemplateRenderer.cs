using NotificationService.Application.Common.Interfaces;
using NotificationService.Domain.Exceptions;
using Scriban;
using Scriban.Runtime;

namespace NotificationService.Infrastructure.Templating;

/// <summary>
/// Renders {{variable}} template source using Scriban — sandboxed by
/// default (no file, network, or reflection access from template text), so
/// template content can safely be authored/edited by non-developers (admin
/// console "Templates" screen) without becoming a code-injection vector.
/// </summary>
public sealed class ScribanTemplateRenderer : ITemplateRenderer
{
    public string Render(string templateSource, IReadOnlyDictionary<string, object?> variables)
    {
        var template = Template.Parse(templateSource);
        if (template.HasErrors)
        {
            var reason = string.Join("; ", template.Messages.Select(m => m.Message));
            throw new TemplateRenderException(templateSource[..Math.Min(40, templateSource.Length)], reason);
        }

        var scriptObject = new ScriptObject();
        foreach (var (key, value) in variables)
            scriptObject[key] = value;

        var context = new TemplateContext { MemberRenamer = member => member.Name };
        context.PushGlobal(scriptObject);

        try
        {
            return template.Render(context);
        }
        catch (Exception ex)
        {
            throw new TemplateRenderException(templateSource[..Math.Min(40, templateSource.Length)], ex.Message);
        }
    }
}
