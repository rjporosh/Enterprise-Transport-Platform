using FluentValidation;

namespace NotificationService.Application.Features.Preferences.UpdateRecipientPreference;

public sealed class UpdateRecipientPreferenceValidator : AbstractValidator<UpdateRecipientPreferenceCommand>
{
    public UpdateRecipientPreferenceValidator()
    {
        RuleFor(x => x.RecipientId).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
    }
}
