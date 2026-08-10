namespace PaymentService.Application.Features.Payments.ProcessPayment;

public sealed record ProcessPaymentResponse(Guid PaymentId, string Status, string? ProviderReference);
