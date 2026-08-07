using NotificationService.Application.Common.Interfaces;

namespace NotificationService.UnitTests.TestSupport;

/// <summary>Deterministic stand-in for Scriban in Application-layer tests -- replaces every {{key}} occurrence with the variable's string value via straight text substitution, so handler tests can assert on rendered output without depending on Infrastructure's ScribanTemplateRenderer.</summary>
public sealed class FakeTemplateRenderer : ITemplateRenderer
{
    public string Render(string templateSource, IReadOnlyDictionary<string, object?> variables)
    {
        var result = templateSource;
        foreach (var (key, value) in variables)
            result = result.Replace($"{{{{{key}}}}}", value?.ToString() ?? string.Empty);
        return result;
    }
}
