using FluentValidation;

namespace AuthService.Application.Features.Auth.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        // OWASP ASVS 2.1.1-ish baseline: length over cleverness. No forced
        // "must contain a symbol" rules — those push users toward predictable
        // substitutions and dont meaningfully improve entropy.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(128);

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(30).When(x => x.PhoneNumber is not null);
    }
}
