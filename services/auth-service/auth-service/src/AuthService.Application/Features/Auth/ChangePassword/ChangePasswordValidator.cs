using FluentValidation;

namespace AuthService.Application.Features.Auth.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10).MaximumLength(128);
        RuleFor(x => x).Must(x => x.CurrentPassword != x.NewPassword)
            .WithMessage("New password must be different from the current password.");
    }
}
