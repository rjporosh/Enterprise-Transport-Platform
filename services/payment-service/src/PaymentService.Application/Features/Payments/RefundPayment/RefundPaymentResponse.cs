namespace PaymentService.Application.Features.Payments.RefundPayment;

public sealed record RefundPaymentResponse(Guid RefundId, string RefundStatus, decimal RefundAmount, string Currency);
