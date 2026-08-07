using FluentValidation;

namespace NotificationService.Application.Features.Templates.GetTemplates;

public sealed class GetTemplatesValidator : AbstractValidator<GetTemplatesQuery>
{
    public GetTemplatesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
