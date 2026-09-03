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
        // The Default provider has no real backend to confirm against. It must
        // NOT report success — a payment method with no configured provider
        // (Card / Cash / MobileWallet / BankTransfer) can only be settled by an
        // operator through an audited endpoint, never auto-confirmed.
        _logger.LogWarning(
            "ConfirmAsync called on the Default provider for {PaymentId}. Root cause: the payment method has no " +
            "configured provider. Possible solution: configure Bkash/Nagad/Stripe/Qr, or settle it manually via an " +
            "audited admin action. Returning Unknown — the payment stays pending-verification.",
            providerPaymentId);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Unknown,
            ProviderReference: providerPaymentId,
            ErrorCode: "no_provider",
            ErrorMessage: "No payment provider is configured for this method."));
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
        _logger.LogWarning("VerifyPaymentMethodAsync on the Default provider for {AccountNumber} — no provider to verify against.", accountNumber);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Failed,
            ErrorCode: "not_supported",
            ErrorMessage: "No payment provider is configured for this method; account verification is unavailable."));
    }

    public bool VerifyWebhookSignature(string payload, string? signatureHeader, string? timestampHeader)
    {
        // Fail closed. A webhook for an unconfigured provider cannot be trusted
        // (P0-6) — accepting any signature let a forged callback mark a payment
        // succeeded.
        _logger.LogWarning("Webhook rejected: no provider configured to verify its signature.");
        return false;
    }
}
