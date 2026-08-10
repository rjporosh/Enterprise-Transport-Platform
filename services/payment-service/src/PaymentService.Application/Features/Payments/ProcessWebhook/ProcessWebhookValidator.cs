using FluentValidation;

namespace PaymentService.Application.Features.Payments.ProcessWebhook;

public class ProcessWebhookValidator : AbstractValidator<ProcessWebhookCommand>
{
    public ProcessWebhookValidator()
    {
        RuleFor(x => x.ProviderName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EventId).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Payload).NotEmpty();
        RuleFor(x => x.Timestamp).NotEmpty();
    }
}
