using System.Net.Http.Headers;
using Platform.SharedKernel.Correlation;

namespace Platform.Common.Correlation;

/// <summary>
/// Outbound <see cref="DelegatingHandler"/> that stamps the ambient correlation
/// id (<see cref="CorrelationContext.Current"/>) onto every request a typed
/// <see cref="HttpClient"/> makes, so a call chain stays traceable across
/// service hops (.ai/MASTER-RULES.md §39).
///
/// Register with: <c>services.AddTransient&lt;CorrelationPropagationHandler&gt;()</c>
/// then <c>.AddHttpClient&lt;T&gt;().AddHttpMessageHandler&lt;CorrelationPropagationHandler&gt;()</c>.
/// </summary>
public sealed class CorrelationPropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationContext.Current;
        if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains(PlatformHeaders.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(PlatformHeaders.CorrelationId, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
