using FluentValidation;

namespace AuthService.Application.Features.Auth.Otp;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6);
        RuleFor(x => x.Channel).NotEmpty().Must(c => c == "email" || c == "sms");
    }
}
