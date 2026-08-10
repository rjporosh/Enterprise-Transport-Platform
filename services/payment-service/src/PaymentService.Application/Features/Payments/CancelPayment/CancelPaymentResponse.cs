namespace PaymentService.Application.Features.Payments.CancelPayment;

public sealed record CancelPaymentResponse(Guid PaymentId, string Status);
