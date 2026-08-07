using FluentValidation;

namespace NotificationService.Application.Features.Templates.UpdateTemplate;

public sealed class UpdateTemplateValidator : AbstractValidator<UpdateTemplateCommand>
{
    public UpdateTemplateValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(65536);
        RuleFor(x => x.Subject).MaximumLength(500);
    }
}
