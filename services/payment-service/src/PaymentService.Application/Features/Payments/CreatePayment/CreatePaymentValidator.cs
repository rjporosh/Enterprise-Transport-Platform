using FluentValidation;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Features.Payments.CreatePayment;

public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderReference).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.FeeAmount).GreaterThanOrEqualTo(0).When(x => x.FeeAmount.HasValue);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).When(x => x.TaxAmount.HasValue);
        RuleFor(x => x.TtlMinutes).GreaterThan(0).When(x => x.TtlMinutes.HasValue);
    }
}
