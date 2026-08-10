using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace PaymentService.Infrastructure.Providers;

public class DefaultPaymentProvider : IPaymentProvider
{
    public string ProviderName => "Default";
    private readonly ILogger<DefaultPaymentProvider> _logger;

    public DefaultPaymentProvider(ILogger<DefaultPaymentProvider> logger)
    {
        _logger = logger;
    }

    public Task<PaymentProviderResult> ProcessAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment {PaymentId} with default provider", request.ProviderPaymentId);

        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Processing,
            ProviderReference: request.ProviderPaymentId,
            RawResponse: new Dictionary<string, string>
            {
                ["message"] = "Default provider - process manually"
            }));
    }

    public Task<PaymentProviderResult> ConfirmAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Confirming payment {PaymentId} with default provider", providerPaymentId);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Succeeded,
            ProviderTransactionId: providerPaymentId,
            ProviderReference: providerPaymentId));
    }

    public Task<PaymentProviderResult> FailAsync(string providerPaymentId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Failing payment {PaymentId} with default provider: {Reason}", providerPaymentId, reason);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Failed,
            ErrorCode: "default_failure",
            ErrorMessage: reason));
    }

    public Task<PaymentProviderResult> RefundAsync(RefundProviderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refunding payment {PaymentId} with default provider", request.ProviderPaymentId);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Processing,
            ProviderReference: request.ProviderPaymentId,
            RawResponse: new Dictionary<string, string>
            {
                ["message"] = "Default provider - refund processed manually"
            }));
    }

    public Task<PaymentProviderResult> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting status for payment {PaymentId} with default provider", providerPaymentId);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Unknown,
            ProviderReference: providerPaymentId));
    }

    public Task<PaymentProviderResult> VerifyPaymentMethodAsync(string accountNumber, string? metadata = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Verifying account {AccountNumber} with default provider", accountNumber);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Succeeded,
            ProviderReference: accountNumber,
            RawResponse: new Dictionary<string, string>
            {
                ["message"] = "Default provider - verification skipped"
            }));
    }

    public bool VerifyWebhookSignature(string payload, string? signatureHeader, string? timestampHeader)
    {
        return true;
    }
}
