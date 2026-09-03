using System.Text.Json.Serialization;
using MediatR;

namespace PaymentService.Application.Features.Payments.GenerateQr;

/// <summary>
/// Moves a Pending payment to Processing and returns a genuine EMVCo
/// merchant-presented ("Bangla QR") payload for the customer to scan. Idempotent
/// while the payment is Processing — re-calling returns the same QR.
/// </summary>
public sealed record GenerateQrCommand(
    Guid PaymentId,
    [property: JsonIgnore] CancellationToken CancellationToken = default) : IRequest<GenerateQrResponse>;

public sealed record GenerateQrResponse(
    Guid PaymentId,
    string Status,
    string QrPayload,
    string QrImageDataUri,
    DateTimeOffset ExpiresAtUtc);
