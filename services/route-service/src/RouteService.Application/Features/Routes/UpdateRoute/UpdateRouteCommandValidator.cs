using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Routes.UpdateRoute;

public sealed class UpdateRouteCommandValidator : AbstractValidator<UpdateRouteCommand>
{
    public UpdateRouteCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage(localization.GetString("Route.NameRequired"));
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.EstimatedDuration).GreaterThan(TimeSpan.Zero);
    }
}
