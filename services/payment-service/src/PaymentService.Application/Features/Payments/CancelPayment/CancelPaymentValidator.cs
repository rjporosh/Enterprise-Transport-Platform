using FluentValidation;

namespace PaymentService.Application.Features.Payments.CancelPayment;

public class CancelPaymentValidator : AbstractValidator<CancelPaymentCommand>
{
    public CancelPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
