namespace PaymentService.Application.Features.Payments.ConfirmPayment;

public sealed record ConfirmPaymentResponse(Guid PaymentId, string Status, string ProviderTransactionId);
