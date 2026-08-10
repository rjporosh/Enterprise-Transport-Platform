using FluentValidation;

namespace PaymentService.Application.Features.Payments.RefundPayment;

public class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.InitiatedByUserId).MaximumLength(200);
    }
}
