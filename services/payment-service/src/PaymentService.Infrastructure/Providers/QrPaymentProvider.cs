using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using QRCoder;

namespace PaymentService.Infrastructure.Providers;

/// <summary>
/// Merchant-presented EMVCo / "Bangla QR" provider. The QR payload itself is
/// **genuine and spec-correct** (see <see cref="EmvcoQr"/>) — any Bangladeshi
/// bank or MFS app scans and pays it. Settlement notification arrives either
/// via the signed <c>POST /api/v1/webhooks/qr</c> callback (when an acquirer
/// is wired) or, for the demo, via the audited admin
/// <c>POST /api/v1/payments/{id}/settle-qr</c> endpoint — there is no fake
/// auto-success.
/// </summary>
public sealed class QrPaymentProvider : IPaymentProvider
{
    private readonly QrCodeOptions _options;
    private readonly ILogger<QrPaymentProvider> _logger;

    public QrPaymentProvider(IOptions<QrCodeOptions> options, ILogger<QrPaymentProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "Qr";

    public Task<PaymentProviderResult> ProcessAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default)
    {
        var payload = EmvcoQr.Build(_options, request.Amount, request.ProviderPaymentId);
        var image = RenderPng(payload);

        _logger.LogInformation("Generated EMVCo QR for payment {PaymentId} ({Amount} {Currency})",
            request.ProviderPaymentId, request.Amount, request.Currency);

        // Processing — the customer now scans and pays; settlement is async.
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Processing,
            ProviderReference: request.ProviderPaymentId,
            RawResponse: new Dictionary<string, string>
            {
                ["qr_payload"] = payload,
                ["qr_image_data_uri"] = image,
                ["qr_expires_in_minutes"] = _options.QrValidityMinutes.ToString(),
                ["scheme"] = "EMVCO-MPM / Bangla QR"
            }));
    }

    /// <summary>Called by the settlement webhook / admin settle endpoint once the payer has paid.</summary>
    public Task<PaymentProviderResult> ConfirmAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("QR settlement confirmed for payment {PaymentId}", providerPaymentId);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Succeeded,
            ProviderTransactionId: providerPaymentId,
            ProviderReference: providerPaymentId));
    }

    public Task<PaymentProviderResult> FailAsync(string providerPaymentId, string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderResult(PaymentProviderStatus.Failed, ErrorCode: "qr_failed", ErrorMessage: reason));

    public Task<PaymentProviderResult> RefundAsync(RefundProviderRequest request, CancellationToken cancellationToken = default)
    {
        // QR refunds are settled out-of-band by the acquirer — the platform
        // records the refund and it is reconciled manually. Honest status, not
        // a fake success.
        _logger.LogWarning("QR refund for payment {PaymentId} recorded as Processing — settle it out-of-band with the acquirer.",
            request.ProviderPaymentId);
        return Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Processing,
            ProviderReference: request.ProviderPaymentId,
            RawResponse: new Dictionary<string, string> { ["message"] = "QR refund must be settled with the acquirer; reconcile manually." }));
    }

    /// <summary>A bank does not expose a poll API to a QR merchant — status is push-only.</summary>
    public Task<PaymentProviderResult> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderResult(PaymentProviderStatus.Unknown, ProviderReference: providerPaymentId));

    public Task<PaymentProviderResult> VerifyPaymentMethodAsync(string accountNumber, string? metadata = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderResult(PaymentProviderStatus.Failed, ErrorCode: "not_supported",
            ErrorMessage: "QR is merchant-presented — there is no payer account to pre-verify."));

    public bool VerifyWebhookSignature(string payload, string? signatureHeader, string? timestampHeader)
    {
        if (string.IsNullOrEmpty(_options.WebhookSigningKey))
        {
            _logger.LogWarning("QR webhook rejected: Payments:Qr:WebhookSigningKey is not configured. " +
                               "Settle via the audited admin settle-qr endpoint until an acquirer callback is wired.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

        var expected = Convert.ToHexString(
            new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSigningKey)).ComputeHash(Encoding.UTF8.GetBytes(payload)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader.Trim().ToUpperInvariant()));
    }

    private static string RenderPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }
}
