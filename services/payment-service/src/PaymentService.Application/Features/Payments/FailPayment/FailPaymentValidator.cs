using FluentValidation;

namespace PaymentService.Application.Features.Payments.FailPayment;

public class FailPaymentValidator : AbstractValidator<FailPaymentCommand>
{
    public FailPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FailureCode).MaximumLength(100);
    }
}
