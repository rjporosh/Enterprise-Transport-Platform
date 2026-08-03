using FluentValidation;

namespace AuthService.Application.Features.Auth.RefreshToken;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RawRefreshToken).NotEmpty();
    }
}
