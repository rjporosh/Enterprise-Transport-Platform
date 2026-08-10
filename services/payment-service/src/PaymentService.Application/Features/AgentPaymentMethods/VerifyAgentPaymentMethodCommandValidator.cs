using FluentValidation;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public class VerifyAgentPaymentMethodCommandValidator : AbstractValidator<VerifyAgentPaymentMethodCommand>
{
    public VerifyAgentPaymentMethodCommandValidator()
    {
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.VerificationToken).MaximumLength(200);
    }
}