using PaymentService.Application.Common.Models;

namespace PaymentService.Application.Common.Interfaces;

public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentProviderResult> ProcessAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default);
    Task<PaymentProviderResult> ConfirmAsync(string providerPaymentId, CancellationToken cancellationToken = default);
    Task<PaymentProviderResult> FailAsync(string providerPaymentId, string reason, CancellationToken cancellationToken = default);
    Task<PaymentProviderResult> RefundAsync(RefundProviderRequest request, CancellationToken cancellationToken = default);
    Task<PaymentProviderResult> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken = default);
    Task<PaymentProviderResult> VerifyPaymentMethodAsync(string accountNumber, string? metadata = null, CancellationToken cancellationToken = default);
    bool VerifyWebhookSignature(string payload, string? signatureHeader, string? timestampHeader);
}
