using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Infrastructure.Communication;
using PaymentService.Infrastructure.Providers;

namespace PaymentService.Infrastructure;

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly ILogger<PaymentProviderFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _providerTypes;

    public PaymentProviderFactory(ILogger<PaymentProviderFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _providerTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["Default"] = typeof(DefaultPaymentProvider),
            ["Bkash"] = typeof(BkashPaymentProvider),
            ["Nagad"] = typeof(NagadPaymentProvider),
            ["Stripe"] = typeof(StripePaymentProvider),
            ["Qr"] = typeof(QrPaymentProvider)
        };
    }

    public IReadOnlyCollection<string> AvailableProviders => _providerTypes.Keys.ToList().AsReadOnly();

    public IPaymentProvider GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            providerName = "Default";

        if (_providerTypes.TryGetValue(providerName, out var type))
        {
            try
            {
                var provider = _serviceProvider.GetService(type) as IPaymentProvider;
                if (provider is not null)
                    return provider;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve provider '{Provider}' from DI. Falling back to Default.", providerName);
            }
        }

        _logger.LogWarning("Provider '{Provider}' not found. Falling back to Default.", providerName);
        return _serviceProvider.GetRequiredService<DefaultPaymentProvider>();
    }
}