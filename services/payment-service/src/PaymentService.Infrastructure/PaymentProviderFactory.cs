using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Infrastructure.Providers;

namespace PaymentService.Infrastructure;

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly ILogger<PaymentProviderFactory> _logger;
    private readonly Dictionary<string, IPaymentProvider> _providers;

    public PaymentProviderFactory(ILogger<PaymentProviderFactory> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _providers = new Dictionary<string, IPaymentProvider>(StringComparer.OrdinalIgnoreCase)
        {
            ["Default"] = new DefaultPaymentProvider(loggerFactory.CreateLogger<DefaultPaymentProvider>())
        };
    }

    public IReadOnlyCollection<string> AvailableProviders => _providers.Keys.ToList().AsReadOnly();

    public IPaymentProvider GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            providerName = "Default";

        if (_providers.TryGetValue(providerName, out var provider))
            return provider;

        _logger.LogWarning("Provider '{Provider}' not found. Falling back to Default.", providerName);
        return _providers["Default"];
    }
}
