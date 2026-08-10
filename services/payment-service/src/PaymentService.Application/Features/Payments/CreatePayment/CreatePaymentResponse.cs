namespace PaymentService.Application.Features.Payments.CreatePayment;

public sealed record CreatePaymentResponse(Guid PaymentId, string Status, DateTimeOffset ExpiresAtUtc);
