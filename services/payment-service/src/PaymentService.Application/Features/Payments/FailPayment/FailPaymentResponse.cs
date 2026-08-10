namespace PaymentService.Application.Features.Payments.FailPayment;

public sealed record FailPaymentResponse(Guid PaymentId, string Status, string FailureReason);
