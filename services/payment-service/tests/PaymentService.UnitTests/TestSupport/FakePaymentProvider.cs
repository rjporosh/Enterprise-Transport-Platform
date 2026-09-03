using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;

namespace PaymentService.UnitTests.TestSupport;

/// <summary>Configurable in-test <see cref="IPaymentProvider"/> + a factory that always returns it.</summary>
public sealed class FakePaymentProvider : IPaymentProvider, IPaymentProviderFactory
{
    public string ProviderName => "Fake";

    public PaymentProviderStatus RefundResult { get; set; } = PaymentProviderStatus.Succeeded;
    public PaymentProviderStatus StatusResult { get; set; } = PaymentProviderStatus.Succeeded;
    public bool WebhookSignatureValid { get; set; } = true;
    public bool RefundThrows { get; set; }

    public IPaymentProvider GetProvider(string providerName) => this;
    public IReadOnlyCollection<string> AvailableProviders => new[] { "Fake" };

    public Task<PaymentProviderResult> ProcessAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderResult(PaymentProviderStatus.Processing, ProviderReference: request.ProviderPaymentId));

    public Task<PaymentProviderResult> ConfirmAsync(string providerPaymentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderResult(StatusResult, ProviderTransactionId: providerPaymentId, ProviderReference: providerPaymentId));

    public Task<PaymentProviderResult> FailAsync(string providerPaymentId, string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderResult(PaymentProviderStatus.Failed, ErrorMessage: reason));

    public Task<PaymentProviderResult> RefundAsync(RefundProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (RefundThrows) throw new InvalidOperationException("provider down");
        return Task.FromResult(new PaymentProviderResult(RefundResult, ProviderTransactionId: "refund-txn"));
    }

    public Task<PaymentProviderResult> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderResult(StatusResult, ProviderTransactionId: providerPaymentId, ProviderReference: providerPaymentId));

    public Task<PaymentProviderResult> VerifyPaymentMethodAsync(string accountNumber, string? metadata = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderResult(PaymentProviderStatus.Succeeded));

    public bool VerifyWebhookSignature(string payload, string? signatureHeader, string? timestampHeader) => WebhookSignatureValid;
}
