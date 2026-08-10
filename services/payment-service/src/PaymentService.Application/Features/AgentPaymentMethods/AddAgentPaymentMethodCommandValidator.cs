using FluentValidation;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public class AddAgentPaymentMethodCommandValidator : AbstractValidator<AddAgentPaymentMethodCommand>
{
    public AddAgentPaymentMethodCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.MethodType).IsInEnum();
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AccountName).MaximumLength(200);
        RuleFor(x => x.Metadata).MaximumLength(2000);
    }
}