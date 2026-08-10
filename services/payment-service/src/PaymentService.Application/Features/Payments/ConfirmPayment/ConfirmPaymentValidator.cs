using FluentValidation;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Features.Payments.ConfirmPayment;

public class ConfirmPaymentValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.ProviderTransactionId).NotEmpty().MaximumLength(200);
    }
}
