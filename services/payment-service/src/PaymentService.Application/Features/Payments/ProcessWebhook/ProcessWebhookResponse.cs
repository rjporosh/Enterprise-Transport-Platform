namespace PaymentService.Application.Features.Payments.ProcessWebhook;

public sealed record ProcessWebhookResponse(bool Success, string? PaymentId, string Status, string? Error);
