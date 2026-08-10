namespace PaymentService.Application.Common.Models;

public sealed record PaymentProviderRequest(
    string ProviderPaymentId,
    decimal Amount,
    string Currency,
    string OrderReference,
    Guid CustomerId,
    string PaymentMethod,
    string? IdempotencyKey = null,
    string? CorrelationId = null,
    Dictionary<string, string>? Metadata = null);
