using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Stops.UpdateStop;

public sealed class UpdateStopCommandValidator : AbstractValidator<UpdateStopCommand>
{
    public UpdateStopCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.StopId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage(localization.GetString("Stop.NameRequired"));
        RuleFor(x => x.City).NotEmpty().WithMessage(localization.GetString("Stop.CityRequired"));
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
