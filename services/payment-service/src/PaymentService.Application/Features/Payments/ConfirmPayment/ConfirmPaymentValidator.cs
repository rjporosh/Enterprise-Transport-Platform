using FluentValidation;

namespace PaymentService.Application.Features.Payments.ConfirmPayment;

public class ConfirmPaymentValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        // ProviderTransactionId / ProviderReference are hints only — the confirm
        // decision comes from a server-side provider.GetStatusAsync (P0-5). Bound
        // for length but not required.
        RuleFor(x => x.ProviderTransactionId).MaximumLength(200);
        RuleFor(x => x.ProviderReference).MaximumLength(200);
    }
}
