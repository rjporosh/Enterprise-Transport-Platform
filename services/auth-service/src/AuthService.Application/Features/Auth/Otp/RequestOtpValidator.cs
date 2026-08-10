using FluentValidation;

namespace AuthService.Application.Features.Auth.Otp;

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Channel).NotEmpty().Must(c => c == "email" || c == "sms");
        RuleFor(x => x.Destination).NotEmpty();
    }
}
