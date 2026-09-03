namespace PaymentService.Infrastructure.Providers;

/// <summary>
/// Merchant identity baked into every EMVCo merchant-presented QR
/// ("Bangla QR" — Bangladesh Bank's standardised EMVCo profile). These are
/// the values a real acquirer assigns during merchant onboarding; the demo
/// ships sensible placeholders. See
/// <c>docs/programmers-guide/payments-qr.md</c>.
/// </summary>
public sealed class QrCodeOptions
{
    public const string SectionName = "Payments:Qr";

    /// <summary>Acquirer / scheme reserved-template id (EMVCo tag 26–51). Placeholder AID for the demo.</summary>
    public string MerchantAccountId { get; set; } = "bd.demo.transport";

    /// <summary>Acquiring institution's merchant id under the scheme.</summary>
    public string MerchantId { get; set; } = "DEMO0000001";

    /// <summary>ISO 18245 merchant category code — 4131 = "Bus lines".</summary>
    public string MerchantCategoryCode { get; set; } = "4131";

    /// <summary>Legal/trading name shown in the payer's app (EMVCo tag 59, ≤ 25 chars).</summary>
    public string MerchantName { get; set; } = "Enterprise Transport";

    /// <summary>Merchant city (EMVCo tag 60, ≤ 15 chars).</summary>
    public string MerchantCity { get; set; } = "Dhaka";

    /// <summary>ISO 3166-1 alpha-2.</summary>
    public string CountryCode { get; set; } = "BD";

    /// <summary>ISO 4217 numeric — 050 = BDT.</summary>
    public string TransactionCurrency { get; set; } = "050";

    /// <summary>How long a generated QR is valid for.</summary>
    public int QrValidityMinutes { get; set; } = 15;

    /// <summary>
    /// HMAC key used to sign the settlement webhook (<c>POST /api/v1/webhooks/qr</c>).
    /// When empty the webhook route rejects everything and settlement must go
    /// through the audited admin <c>settle-qr</c> endpoint (documented demo path).
    /// </summary>
    public string WebhookSigningKey { get; set; } = string.Empty;
}
