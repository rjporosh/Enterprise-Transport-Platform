using FluentValidation;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public class SetDefaultPaymentMethodCommandValidator : AbstractValidator<SetDefaultPaymentMethodCommand>
{
    public SetDefaultPaymentMethodCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
    }
}