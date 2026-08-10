namespace PaymentService.Application.Common.Models;

public sealed record RefundProviderRequest(
    string ProviderPaymentId,
    decimal RefundAmount,
    string Currency,
    string? RefundReason = null,
    string? IdempotencyKey = null,
    string? CorrelationId = null);
