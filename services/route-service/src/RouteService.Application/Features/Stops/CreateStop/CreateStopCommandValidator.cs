using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Stops.CreateStop;

public sealed class CreateStopCommandValidator : AbstractValidator<CreateStopCommand>
{
    public CreateStopCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(localization.GetString("Stop.CodeRequired"));
        RuleFor(x => x.Name).NotEmpty().WithMessage(localization.GetString("Stop.NameRequired"));
        RuleFor(x => x.City).NotEmpty().WithMessage(localization.GetString("Stop.CityRequired"));
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
