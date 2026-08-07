using FluentValidation;

namespace NotificationService.Application.Features.Templates.CreateTemplate;

public sealed class CreateTemplateValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200).Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Key may only contain letters, digits, dot, underscore and hyphen.");
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(65536);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500)
            .When(x => x.Channel == NotificationService.Domain.Enums.TemplateChannel.Email)
            .WithMessage("Subject is required for Email templates.");
    }
}
