using System.Text.Json.Serialization;
using MediatR;

namespace PaymentService.Application.Features.Payments.SettleQr;

/// <summary>
/// Records that a QR payment has settled. Two callers:
/// <list type="bullet">
///   <item>the signed acquirer webhook (<c>POST /api/v1/webhooks/qr</c>), once wired;</item>
///   <item>an operator/admin via <c>POST /api/v1/payments/{id}/settle-qr</c> — the
///   audited demo stand-in for a live acquirer callback.</item>
/// </list>
/// Drives the payment to Succeeded through the provider and publishes
/// <c>payment.succeeded</c> (which booking-service turns into a confirmed
/// booking). Idempotent.
/// </summary>
public sealed record SettleQrCommand(
    Guid PaymentId,
    string SettlementReference,
    string SettledBy,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<SettleQrResponse>;

public sealed record SettleQrResponse(Guid PaymentId, string Status, string ProviderTransactionId);
